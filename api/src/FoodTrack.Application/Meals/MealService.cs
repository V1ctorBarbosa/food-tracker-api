using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Meals;

public sealed class MealService
{
    private readonly IMealRepository _mealRepository;
    private readonly IFoodRepository _foodRepository;

    public MealService(IMealRepository mealRepository, IFoodRepository foodRepository)
    {
        _mealRepository = mealRepository;
        _foodRepository = foodRepository;
    }

    public async Task<IReadOnlyList<MealDto>> ListAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var meals = await _mealRepository.ListByUserAsync(userId, from, to, cancellationToken);
        return meals.Select(ToDto).ToList();
    }

    public async Task<MealDto?> GetByIdAsync(UserId userId, MealId id, CancellationToken cancellationToken = default)
    {
        var meal = await _mealRepository.GetByIdAsync(id, userId, cancellationToken);
        if (meal is null)
        {
            return null;
        }

        return ToDto(meal);
    }

    public async Task<MealDto> CreateAsync(UserId userId, CreateMealRequest request, CancellationToken cancellationToken = default)
    {
        var items = await ResolveItemsAsync(request.Items, cancellationToken);
        var mealType = Enum.Parse<MealType>(request.MealType, ignoreCase: true);
        var meal = Meal.Create(MealId.NewId(), userId, DateTimeOffset.UtcNow, mealType, items);

        await _mealRepository.AddAsync(meal, cancellationToken);
        return ToDto(meal);
    }

    /// <returns>The replaced meal, or null when it does not exist or does not belong to <paramref name="userId"/>.</returns>
    public async Task<MealDto?> UpdateAsync(
        UserId userId,
        MealId id,
        UpdateMealRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _mealRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var items = await ResolveItemsAsync(request.Items, cancellationToken);
        var mealType = Enum.Parse<MealType>(request.MealType, ignoreCase: true);
        var meal = Meal.Create(id, userId, existing.LoggedAt, mealType, items);

        await _mealRepository.UpdateAsync(meal, cancellationToken);
        return ToDto(meal);
    }

    /// <returns>True when the meal existed and belonged to <paramref name="userId"/> and was deleted.</returns>
    public async Task<bool> DeleteAsync(UserId userId, MealId id, CancellationToken cancellationToken = default)
    {
        var existing = await _mealRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await _mealRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<MealItem>> ResolveItemsAsync(
        IReadOnlyList<MealItemRequest> itemRequests,
        CancellationToken cancellationToken)
    {
        var items = new List<MealItem>(itemRequests.Count);
        foreach (var itemRequest in itemRequests)
        {
            var foodId = new FoodId(itemRequest.FoodId);
            var food = await _foodRepository.GetByIdAsync(foodId, cancellationToken);
            if (food is null)
            {
                throw new ArgumentException($"Food '{itemRequest.FoodId}' does not exist.", nameof(itemRequests));
            }

            items.Add(ResolveItem(food, itemRequest));
        }

        return items;
    }

    private static MealItem ResolveItem(Food food, MealItemRequest itemRequest)
    {
        if (itemRequest.FoodServingId is { } servingIdValue)
        {
            return ResolveItemFromServing(food, servingIdValue, itemRequest.ServingCount);
        }

        if (itemRequest.Quantity is { } quantity && itemRequest.Unit is { } unitValue)
        {
            return ResolveItemFromQuantity(food, quantity, unitValue);
        }

        throw new ArgumentException(
            "Provide either Quantity+Unit or FoodServingId+ServingCount for each meal item.",
            nameof(itemRequest));
    }

    private static MealItem ResolveItemFromServing(Food food, string servingIdValue, double? servingCount)
    {
        if (servingCount is not { } count)
        {
            throw new ArgumentException("ServingCount is required when FoodServingId is provided.", nameof(servingCount));
        }

        var servingId = new FoodServingId(servingIdValue);
        var serving = food.Servings.FirstOrDefault(candidate => candidate.Id == servingId);
        if (serving is null)
        {
            throw new ArgumentException($"Food '{food.Id.Value}' has no serving '{servingIdValue}'.", nameof(servingIdValue));
        }

        var nutrition = NutritionCalculator.Calculate(food, servingId, count);
        var resolvedQuantity = serving.Quantity * count;

        return MealItem.Create(
            MealItemId.NewId(),
            food.Id,
            resolvedQuantity,
            serving.Unit,
            nutrition.Calories,
            nutrition.Protein,
            nutrition.Carbs,
            nutrition.Fat);
    }

    private static MealItem ResolveItemFromQuantity(Food food, double quantity, string unitValue)
    {
        var unit = Enum.Parse<MeasurementUnit>(unitValue, ignoreCase: true);
        var nutrition = NutritionCalculator.Calculate(food, quantity, unit);

        return MealItem.Create(
            MealItemId.NewId(),
            food.Id,
            quantity,
            unit,
            nutrition.Calories,
            nutrition.Protein,
            nutrition.Carbs,
            nutrition.Fat);
    }

    private static MealDto ToDto(Meal meal) => new(
        meal.Id.Value,
        meal.LoggedAt,
        meal.MealType.ToString(),
        meal.TotalCalories.Value,
        meal.TotalProtein.Value,
        meal.TotalCarbs.Value,
        meal.TotalFat.Value,
        meal.Items.Select(ToDto).ToList());

    private static MealItemDto ToDto(MealItem item) => new(
        item.Id.Value,
        item.FoodId.Value,
        item.Quantity,
        item.Unit.ToString(),
        item.Calories.Value,
        item.Protein.Value,
        item.Carbs.Value,
        item.Fat.Value);
}
