using FoodTrack.Domain.Meals;

namespace FoodTrack.Application.Meals;

public static class MealDtoMapper
{
    /// <param name="photoReadUrls">The read URL of every capture that has a photo.</param>
    public static MealDto ToDto(Meal meal, IReadOnlyDictionary<CaptureId, Uri> photoReadUrls) => new(
        meal.Id.Value,
        meal.LoggedAt,
        meal.MealType.ToString(),
        meal.TotalCalories.Value,
        meal.TotalProtein.Value,
        meal.TotalCarbs.Value,
        meal.TotalFat.Value,
        meal.Items.Select(ToDto).ToList(),
        meal.Captures.Select(capture => ToDto(capture, photoReadUrls)).ToList());

    public static MealSummaryDto ToDto(MealSummary summary) => new(
        summary.Id.Value,
        summary.LoggedAt,
        summary.MealType.ToString(),
        summary.TotalCalories.Value,
        summary.TotalProtein.Value,
        summary.TotalCarbs.Value,
        summary.TotalFat.Value);

    public static MealItemDto ToDto(MealItem item)
    {
        string? foodId = null;
        EstimatedFoodDto? estimatedFood = null;

        switch (item.Origin)
        {
            case CatalogFoodOrigin catalogOrigin:
                foodId = catalogOrigin.FoodId.Value;
                break;
            case EstimatedFoodOrigin estimatedOrigin:
                estimatedFood = ToDto(estimatedOrigin);
                break;
            default:
                throw new InvalidOperationException($"Unknown meal item origin {item.Origin.GetType().Name}.");
        }

        return new MealItemDto(
            item.Id.Value,
            foodId,
            item.Quantity,
            item.Unit.ToString(),
            item.Calories.Value,
            item.Protein.Value,
            item.Carbs.Value,
            item.Fat.Value,
            estimatedFood);
    }

    public static MealCaptureDto ToDto(MealCapture capture, IReadOnlyDictionary<CaptureId, Uri> photoReadUrls)
    {
        string? photoUrl = null;
        if (photoReadUrls.TryGetValue(capture.Id, out var readUrl))
        {
            photoUrl = readUrl.ToString();
        }

        return new MealCaptureDto(capture.Id.Value, capture.HasPhoto, capture.Description?.Value, photoUrl);
    }

    private static EstimatedFoodDto ToDto(EstimatedFoodOrigin estimatedOrigin)
    {
        var estimatedFood = estimatedOrigin.Food;
        var nutritionPerBaseUnit = estimatedFood.NutritionPerBaseUnit;

        return new EstimatedFoodDto(
            estimatedFood.Name,
            estimatedOrigin.CaptureId.Value,
            estimatedFood.BaseUnit.Unit.ToString(),
            nutritionPerBaseUnit.Calories.Value,
            nutritionPerBaseUnit.Protein.Value,
            nutritionPerBaseUnit.Carbs.Value,
            nutritionPerBaseUnit.Fat.Value);
    }
}
