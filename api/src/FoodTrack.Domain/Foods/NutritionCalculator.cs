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
        var foodCategory = food.BaseUnit.Unit.GetCategory();
        if (unit.GetCategory() != foodCategory)
        {
            throw new ArgumentException(
                $"Food '{food.Name}' is measured by {foodCategory.ToString().ToLowerInvariant()}, so it cannot be logged in {unit}.",
                nameof(unit));
        }

        var normalizedQuantity = MeasurementUnitConversion.ConvertTo(quantity, unit, food.BaseUnit.Unit);
        var ratio = normalizedQuantity / food.BaseUnit.Quantity;

        return new NutritionAmount(
            new Kilocalories((int)Math.Round(food.CaloriesPerBaseUnit.Value * ratio)),
            new Grams(food.ProteinPerBaseUnit.Value * ratio),
            new Grams(food.CarbsPerBaseUnit.Value * ratio),
            new Grams(food.FatPerBaseUnit.Value * ratio));
    }
}
