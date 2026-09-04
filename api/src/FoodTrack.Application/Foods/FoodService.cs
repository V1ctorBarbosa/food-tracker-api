using FoodTrack.Domain.Foods;

namespace FoodTrack.Application.Foods;

public sealed class FoodService
{
    private readonly IFoodRepository _foodRepository;
    private readonly IExternalFoodSource _externalFoodSource;

    public FoodService(IFoodRepository foodRepository, IExternalFoodSource externalFoodSource)
    {
        _foodRepository = foodRepository;
        _externalFoodSource = externalFoodSource;
    }

    public async Task<FoodDto?> GetByIdAsync(FoodId id, CancellationToken cancellationToken = default)
    {
        var food = await _foodRepository.GetByIdAsync(id, cancellationToken);
        if (food is null)
        {
            return null;
        }

        return ToDto(food);
    }

    /// <summary>
    /// Searches the local cache first (docs section 14 flow). On a cache miss, falls back to
    /// the external nutritional source, caches every result it returns, and serves those —
    /// so the next search for the same food is a cache hit.
    /// </summary>
    public async Task<IReadOnlyList<FoodDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var cachedFoods = await _foodRepository.SearchByNameAsync(query, cancellationToken);
        if (cachedFoods.Count > 0)
        {
            return cachedFoods.Select(ToDto).ToList();
        }

        var externalFoods = await _externalFoodSource.SearchAsync(query, cancellationToken);
        foreach (var food in externalFoods)
        {
            await _foodRepository.SaveAsync(food, cancellationToken);
        }

        return externalFoods.Select(ToDto).ToList();
    }

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
        food.Servings.Select(s => new FoodServingDto(s.Id.Value, s.Name, s.Quantity, s.Unit.ToString())).ToList());
}
