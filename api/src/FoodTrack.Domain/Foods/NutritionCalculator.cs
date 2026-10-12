using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Foods;

/// <summary>
/// Computes the nutritional values for a quantity of a <see cref="Food"/> (docs section 19).
/// Pure domain logic — no persistence, no framework dependencies.
/// </summary>
public static class NutritionCalculator
{
    /// <exception cref="ArgumentException">
    /// <paramref name="unit"/> measures something else than the food's base unit (grams of a drink, ml of a food).
    /// </exception>
    public static NutritionAmount Calculate(Food food, double quantity, MeasurementUnit unit)
    {
        var nutritionPerBaseUnit = new NutritionAmount(
            food.CaloriesPerBaseUnit,
            food.ProteinPerBaseUnit,
            food.CarbsPerBaseUnit,
            food.FatPerBaseUnit);

        return Calculate(food.Name, food.BaseUnit, nutritionPerBaseUnit, quantity, unit);
    }

    /// <summary>
    /// The same calculation for anything described by nutrition per base unit, so a food that never
    /// became a <see cref="Food"/> (an AI estimate) is scaled exactly like one that did.
    /// </summary>
    /// <param name="measuredName">Names what is being measured in the error message.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="unit"/> measures something else than <paramref name="baseUnit"/> (grams of a drink, ml of a food).
    /// </exception>
    public static NutritionAmount Calculate(
        string measuredName,
        BaseUnit baseUnit,
        NutritionAmount nutritionPerBaseUnit,
        double quantity,
        MeasurementUnit unit)
    {
        var baseCategory = baseUnit.Unit.GetCategory();
        if (unit.GetCategory() != baseCategory)
        {
            throw new ArgumentException(
                $"Food '{measuredName}' is measured by {baseCategory.ToString().ToLowerInvariant()}, so it cannot be logged in {unit}.",
                nameof(unit));
        }

        var normalizedQuantity = MeasurementUnitConversion.ConvertTo(quantity, unit, baseUnit.Unit);
        var ratio = normalizedQuantity / baseUnit.Quantity;

        return new NutritionAmount(
            new Kilocalories((int)Math.Round(nutritionPerBaseUnit.Calories.Value * ratio)),
            new Grams(nutritionPerBaseUnit.Protein.Value * ratio),
            new Grams(nutritionPerBaseUnit.Carbs.Value * ratio),
            new Grams(nutritionPerBaseUnit.Fat.Value * ratio));
    }
}
