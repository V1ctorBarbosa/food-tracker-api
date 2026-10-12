using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Application.MealEstimates;

/// <summary>
/// Turns the estimator's answer into meal items, keeping only foods the domain accepts. A model
/// can answer with an impossible value; that one food is dropped instead of failing the whole
/// estimate, and it never reaches a meal.
/// </summary>
public static class RecognizedFoodMapper
{
    public static IReadOnlyList<MealItem> ToValidItems(IReadOnlyList<RecognizedFood> recognizedFoods, CaptureId captureId) =>
        recognizedFoods
            .Select(recognizedFood => TryToItem(recognizedFood, captureId))
            .OfType<MealItem>()
            .Take(Meal.MaximumItemCount)
            .ToList();

    /// <returns>The item, or null when the domain rejects any of the recognized values.</returns>
    public static MealItem? TryToItem(RecognizedFood recognizedFood, CaptureId captureId)
    {
        try
        {
            return ToItem(recognizedFood, captureId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static MealItem ToItem(RecognizedFood recognizedFood, CaptureId captureId)
    {
        var baseUnit = Enum.Parse<MeasurementUnit>(recognizedFood.BaseUnit, ignoreCase: true);
        var nutritionPerBaseUnit = new NutritionAmount(
            new Kilocalories((int)Math.Round(recognizedFood.CaloriesPerBaseUnit)),
            new Grams(recognizedFood.ProteinPerBaseUnit),
            new Grams(recognizedFood.CarbsPerBaseUnit),
            new Grams(recognizedFood.FatPerBaseUnit));

        var estimatedFood = EstimatedFood.Create(recognizedFood.Name, baseUnit, nutritionPerBaseUnit);

        return MealItem.FromEstimatedFood(MealItemId.NewId(), estimatedFood, captureId, recognizedFood.Quantity, baseUnit);
    }
}
