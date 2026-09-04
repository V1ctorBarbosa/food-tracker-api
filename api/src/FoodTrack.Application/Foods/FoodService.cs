using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Foods;

public sealed class FoodService
{
    // Guards against a search-as-you-type frontend firing one request per keystroke with no
    // debounce -- "c", "ch", "chi" would each be a cache miss and hit the external source.
    // This is a backend safety net regardless of client; the real fix is debouncing client-side.
    private const int MinimumQueryLength = 3;

    private readonly IFoodRepository _foodRepository;
    private readonly IExternalFoodSource _externalFoodSource;

    public FoodService(IFoodRepository foodRepository, IExternalFoodSource externalFoodSource)
    {
        _foodRepository = foodRepository;
        _externalFoodSource = externalFoodSource;
    }

    public async Task<FoodDto?> GetByIdAsync(FoodId id, UserId userId, CancellationToken cancellationToken = default)
    {
        var food = await _foodRepository.GetByIdAsync(id, cancellationToken);
        if (food is null || !IsVisibleTo(food, userId))
        {
            return null;
        }

        return ToDto(food);
    }

    /// <summary>
    /// Searches the local cache first (docs section 14 flow). On a cache miss, falls back to
    /// the external nutritional source, caches every result it returns, and serves those —
    /// so the next search for the same food is a cache hit. Either way, results are filtered to
    /// what <paramref name="userId"/> may see: the shared cache plus their own custom foods,
    /// never another user's.
    /// </summary>
    public async Task<IReadOnlyList<FoodDto>> SearchAsync(UserId userId, string query, CancellationToken cancellationToken = default)
    {
        if (query.Trim().Length < MinimumQueryLength)
        {
            throw new ArgumentException($"Query must be at least {MinimumQueryLength} characters.", nameof(query));
        }

        var cachedFoods = await _foodRepository.SearchByNameAsync(query, cancellationToken);
        if (cachedFoods.Count > 0)
        {
            return FilterVisible(cachedFoods, userId).Select(ToDto).ToList();
        }

        var externalFoods = await _externalFoodSource.SearchAsync(query, cancellationToken);
        foreach (var food in externalFoods)
        {
            await _foodRepository.SaveAsync(food, cancellationToken);
        }

        return FilterVisible(externalFoods, userId).Select(ToDto).ToList();
    }

    public async Task<FoodDto> CreateCustomAsync(UserId userId, CreateCustomFoodRequest request, CancellationToken cancellationToken = default)
    {
        var food = Food.Create(
            FoodId.NewId(),
            request.Name,
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(request.Calories),
            new Grams(request.Protein),
            new Grams(request.Carbs),
            new Grams(request.Fat),
            ownerId: userId);

        await _foodRepository.SaveAsync(food, cancellationToken);
        return ToDto(food);
    }

    private static IEnumerable<Food> FilterVisible(IEnumerable<Food> foods, UserId userId) =>
        foods.Where(food => IsVisibleTo(food, userId));

    private static bool IsVisibleTo(Food food, UserId userId) => food.OwnerId is null || food.OwnerId == userId;

    private static FoodDto ToDto(Food food) => new(
        food.Id.Value,
        food.Name,
        food.Source.ToString(),
        food.BaseUnit.Quantity,
        food.BaseUnit.Unit.ToString(),
        food.CaloriesPerBaseUnit.Value,
        food.ProteinPerBaseUnit.Value,
        food.CarbsPerBaseUnit.Value,
        food.FatPerBaseUnit.Value,
        food.Servings
            .Select(serving => new FoodServingDto(serving.Id.Value, serving.Name, serving.Quantity, serving.Unit.ToString()))
            .ToList());
}
