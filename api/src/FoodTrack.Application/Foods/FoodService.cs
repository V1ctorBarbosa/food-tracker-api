using FoodTrack.Domain.Foods;

namespace FoodTrack.Application.Foods;

public sealed class FoodService
{
    private readonly IFoodRepository _foodRepository;

    public FoodService(IFoodRepository foodRepository)
    {
        _foodRepository = foodRepository;
    }

    public async Task<FoodDto?> GetByIdAsync(FoodId id, CancellationToken cancellationToken = default)
    {
        var food = await _foodRepository.GetByIdAsync(id, cancellationToken);
        return food is null ? null : ToDto(food);
    }

    /// <summary>
    /// Searches the local cache only. When the external nutritional source is wired in,
    /// this is where a cache miss will fall back to it and save the result — the endpoint
    /// and this method's signature do not need to change for that.
    /// </summary>
    public async Task<IReadOnlyList<FoodDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var foods = await _foodRepository.SearchByNameAsync(query, cancellationToken);
        return foods.Select(ToDto).ToList();
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
