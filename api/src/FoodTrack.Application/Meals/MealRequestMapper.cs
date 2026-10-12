using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Application.Meals;

/// <summary>Turns the parts of a meal request that need no database lookup into domain values.</summary>
public static class MealRequestMapper
{
    public static MealCapture ToCapture(MealCaptureRequest captureRequest)
    {
        MealDescription? description = null;
        if (!string.IsNullOrWhiteSpace(captureRequest.Description))
        {
            description = new MealDescription(captureRequest.Description);
        }

        return MealCapture.Create(new CaptureId(captureRequest.Id), captureRequest.HasPhoto, description);
    }

    public static IReadOnlyList<MealCapture> ToCaptures(IReadOnlyList<MealCaptureRequest> captureRequests) =>
        captureRequests.Select(ToCapture).ToList();

    /// <exception cref="ArgumentException">A value is missing, unknown or outside what an estimated food allows.</exception>
    public static MealItem ToEstimatedItem(EstimatedFoodRequest estimatedFoodRequest, double quantity, string unit)
    {
        var nutritionPerBaseUnit = new NutritionAmount(
            new Kilocalories(estimatedFoodRequest.CaloriesPerBaseUnit),
            new Grams(estimatedFoodRequest.ProteinPerBaseUnit),
            new Grams(estimatedFoodRequest.CarbsPerBaseUnit),
            new Grams(estimatedFoodRequest.FatPerBaseUnit));

        var estimatedFood = EstimatedFood.Create(
            estimatedFoodRequest.Name,
            ParseUnit(estimatedFoodRequest.BaseUnit),
            nutritionPerBaseUnit);

        return MealItem.FromEstimatedFood(
            MealItemId.NewId(),
            estimatedFood,
            new CaptureId(estimatedFoodRequest.CaptureId),
            quantity,
            ParseUnit(unit));
    }

    /// <exception cref="ArgumentException">The unit is missing or not a <see cref="MeasurementUnit"/> name.</exception>
    public static MeasurementUnit ParseUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("A unit is required for every meal item.", nameof(unit));
        }

        return Enum.Parse<MeasurementUnit>(unit, ignoreCase: true);
    }
}
