using FoodTrack.Application.Meals;
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

    // No food name is this long; a longer query can only be noise sent to the external sources.
    private const int MaximumQueryLength = 100;

    // Applies only to names users type; names from the external sources (product plus brands) are
    // stored as the source gives them.
    private const int MaximumCustomFoodNameLength = 100;

    private readonly IFoodRepository _foodRepository;
    private readonly IExternalFoodSource _externalFoodSource;
    private readonly IMealRepository _mealRepository;

    public FoodService(IFoodRepository foodRepository, IExternalFoodSource externalFoodSource, IMealRepository mealRepository)
    {
        _foodRepository = foodRepository;
        _externalFoodSource = externalFoodSource;
        _mealRepository = mealRepository;
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
    /// Only shared foods count as a cache hit. Custom foods live in the same collection, so if
    /// they counted, any user could name a custom food after a term and make that search return
    /// nothing for everyone else, since the external source would never be asked again.
    /// </summary>
    public async Task<IReadOnlyList<FoodDto>> SearchAsync(UserId userId, string query, CancellationToken cancellationToken = default)
    {
        var trimmedQueryLength = query.Trim().Length;
        if (trimmedQueryLength < MinimumQueryLength)
        {
            throw new ArgumentException($"Query must be at least {MinimumQueryLength} characters.", nameof(query));
        }

        if (trimmedQueryLength > MaximumQueryLength)
        {
            throw new ArgumentException($"Query must be at most {MaximumQueryLength} characters.", nameof(query));
        }

        var cachedFoods = await _foodRepository.SearchByNameAsync(query, cancellationToken);
        var visibleCachedFoods = FilterVisible(cachedFoods, userId).ToList();
        if (visibleCachedFoods.Any(IsShared))
        {
            return visibleCachedFoods.Select(ToDto).ToList();
        }

        var externalFoods = await FetchAndCacheExternalFoodsAsync(query, cancellationToken);
        return visibleCachedFoods.Concat(externalFoods).Select(ToDto).ToList();
    }

    /// <summary>The custom foods the user created, by name.</summary>
    public async Task<IReadOnlyList<FoodDto>> ListCustomAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var customFoods = await _foodRepository.ListCustomFoodsOwnedByAsync(userId, cancellationToken);

        return customFoods
            .OrderBy(food => food.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(ToDto)
            .ToList();
    }

    /// <summary>
    /// Replaces the name, the base unit and the nutrition of a custom food. When the base unit moves
    /// between g and ml, the items already logged for the food move with it first, so a meal holding
    /// them can still be edited; if saving the food then fails, retrying the same edit finishes the job.
    /// </summary>
    /// <returns>
    /// The updated food, or null when it does not exist, is not a custom food or belongs to someone
    /// else. The three cases look the same to the caller, so one user cannot probe another's foods.
    /// </returns>
    public async Task<FoodDto?> UpdateCustomAsync(
        UserId userId,
        FoodId id,
        UpdateCustomFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCustomFoodNameIsAllowed(request.Name);

        var existingFood = await _foodRepository.GetByIdAsync(id, cancellationToken);
        if (existingFood is null || !IsEditableBy(existingFood, userId))
        {
            return null;
        }

        var baseUnit = CustomFoodBaseUnit.Resolve(request.BaseUnitType, existingFood.BaseUnit.Unit);
        var updatedFood = Food.Create(
            existingFood.Id,
            request.Name,
            existingFood.Source,
            existingFood.ExternalId,
            baseUnit,
            new Kilocalories(request.Calories),
            new Grams(request.Protein),
            new Grams(request.Carbs),
            new Grams(request.Fat),
            existingFood.OwnerId);

        await ReclassifyLoggedItemsIfTheCategoryChangedAsync(existingFood, updatedFood, cancellationToken);
        await _foodRepository.SaveAsync(updatedFood, cancellationToken);
        return ToDto(updatedFood);
    }

    public async Task<FoodDto> CreateCustomAsync(UserId userId, CreateCustomFoodRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCustomFoodNameIsAllowed(request.Name);
        var baseUnit = CustomFoodBaseUnit.Resolve(request.BaseUnitType, MeasurementUnit.Gram);

        var food = Food.Create(
            FoodId.NewId(),
            request.Name,
            FoodSource.Custom,
            externalId: null,
            baseUnit,
            new Kilocalories(request.Calories),
            new Grams(request.Protein),
            new Grams(request.Carbs),
            new Grams(request.Fat),
            ownerId: userId);

        await _foodRepository.SaveAsync(food, cancellationToken);
        return ToDto(food);
    }

    /// <summary>
    /// Saves an externally-fetched food, reusing an existing cached FoodId for the same
    /// Source+ExternalId when one exists -- otherwise every repeated search for the same
    /// external food (e.g. "arroz" today, "arroz cozido" tomorrow) caches it again under a
    /// freshly minted id, duplicating it in every future search result.
    /// </summary>
    private async Task<Food> SaveDeduplicatedAsync(Food food, CancellationToken cancellationToken)
    {
        var foodToSave = await ReuseCachedIdAsync(food, cancellationToken);
        await _foodRepository.SaveAsync(foodToSave, cancellationToken);
        return foodToSave;
    }

    private async Task<Food> ReuseCachedIdAsync(Food food, CancellationToken cancellationToken)
    {
        if (food.ExternalId is null)
        {
            return food;
        }

        var existingFood = await _foodRepository.GetBySourceAndExternalIdAsync(food.Source, food.ExternalId, cancellationToken);
        if (existingFood is null)
        {
            return food;
        }

        return WithId(food, existingFood.Id);
    }

    private static Food WithId(Food food, FoodId id) => Food.Create(
        id,
        food.Name,
        food.Source,
        food.ExternalId,
        food.BaseUnit,
        food.CaloriesPerBaseUnit,
        food.ProteinPerBaseUnit,
        food.CarbsPerBaseUnit,
        food.FatPerBaseUnit,
        food.OwnerId);

    private async Task<IReadOnlyList<Food>> FetchAndCacheExternalFoodsAsync(string query, CancellationToken cancellationToken)
    {
        var externalFoods = await _externalFoodSource.SearchAsync(query, cancellationToken);
        var savedFoods = new List<Food>();
        foreach (var food in externalFoods)
        {
            savedFoods.Add(await SaveDeduplicatedAsync(food, cancellationToken));
        }

        return savedFoods;
    }

    private async Task ReclassifyLoggedItemsIfTheCategoryChangedAsync(
        Food existingFood,
        Food updatedFood,
        CancellationToken cancellationToken)
    {
        var updatedCategory = updatedFood.BaseUnit.Unit.GetCategory();
        if (existingFood.BaseUnit.Unit.GetCategory() == updatedCategory)
        {
            return;
        }

        await _mealRepository.ReclassifyItemUnitsAsync(updatedFood.Id, updatedCategory, cancellationToken);
    }

    private static void EnsureCustomFoodNameIsAllowed(string? name)
    {
        if (name is not null && name.Trim().Length > MaximumCustomFoodNameLength)
        {
            throw new ArgumentException($"Name cannot be longer than {MaximumCustomFoodNameLength} characters.", nameof(name));
        }
    }

    private static bool IsEditableBy(Food food, UserId userId) => food.Source == FoodSource.Custom && food.OwnerId == userId;

    private static bool IsShared(Food food) => food.OwnerId is null;

    private static IEnumerable<Food> FilterVisible(IEnumerable<Food> foods, UserId userId) =>
        foods.Where(food => IsVisibleTo(food, userId));

    private static bool IsVisibleTo(Food food, UserId userId) => food.IsVisibleTo(userId);

    private static FoodDto ToDto(Food food) => new(
        food.Id.Value,
        food.Name,
        food.Source.ToString(),
        food.BaseUnit.Quantity,
        food.BaseUnit.Unit.ToString(),
        food.CaloriesPerBaseUnit.Value,
        food.ProteinPerBaseUnit.Value,
        food.CarbsPerBaseUnit.Value,
        food.FatPerBaseUnit.Value);
}
