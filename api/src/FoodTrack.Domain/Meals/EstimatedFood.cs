using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A food the AI recognized in a meal, with nutrition it estimated per 100 g or 100 ml. It never
/// becomes a catalog <see cref="Food"/>: it lives only inside the <see cref="MealItem"/> it was
/// logged as. Keeping the values per base unit, not as totals, is what lets the item's quantity
/// be edited later and recalculated exactly like a catalog item.
/// </summary>
public sealed record EstimatedFood
{
    public const int MaximumNameLength = 100;

    public const double BaseQuantity = 100;

    // Pure fat is about 900 kcal per 100 g, and protein, carbs and fat together cannot weigh more
    // than the 100 g they are part of. Anything above either is a bad estimate, not a food.
    private const int MaximumCaloriesPerBaseUnit = 900;
    private const double MaximumMacronutrientsPerBaseUnit = 100;

    public string Name { get; }

    public BaseUnit BaseUnit { get; }

    public NutritionAmount NutritionPerBaseUnit { get; }

    private EstimatedFood(string name, BaseUnit baseUnit, NutritionAmount nutritionPerBaseUnit)
    {
        Name = name;
        BaseUnit = baseUnit;
        NutritionPerBaseUnit = nutritionPerBaseUnit;
    }

    /// <param name="baseUnit">Gram for a food, Milliliter for a drink; the base quantity is always <see cref="BaseQuantity"/>.</param>
    /// <exception cref="ArgumentException">The name is empty or too long, or the nutrition is not physically possible.</exception>
    public static EstimatedFood Create(string name, MeasurementUnit baseUnit, NutritionAmount nutritionPerBaseUnit)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        EnsureNameIsValid(trimmedName);
        EnsureNutritionIsPossible(nutritionPerBaseUnit);

        return new EstimatedFood(trimmedName, new BaseUnit(BaseQuantity, baseUnit), nutritionPerBaseUnit);
    }

    /// <exception cref="ArgumentException"><paramref name="unit"/> measures something else than the base unit.</exception>
    public NutritionAmount CalculateFor(double quantity, MeasurementUnit unit) =>
        NutritionCalculator.Calculate(Name, BaseUnit, NutritionPerBaseUnit, quantity, unit);

    private static void EnsureNameIsValid(string name)
    {
        if (name.Length == 0)
        {
            throw new ArgumentException("Estimated food name cannot be empty.", nameof(name));
        }

        if (name.Length > MaximumNameLength)
        {
            throw new ArgumentException($"Estimated food name cannot be longer than {MaximumNameLength} characters.", nameof(name));
        }
    }

    private static void EnsureNutritionIsPossible(NutritionAmount nutritionPerBaseUnit)
    {
        if (nutritionPerBaseUnit.Calories.Value > MaximumCaloriesPerBaseUnit)
        {
            throw new ArgumentException(
                $"An estimated food cannot have more than {MaximumCaloriesPerBaseUnit} kcal per {BaseQuantity}.",
                nameof(nutritionPerBaseUnit));
        }

        var macronutrientsTotal = nutritionPerBaseUnit.Protein.Value + nutritionPerBaseUnit.Carbs.Value + nutritionPerBaseUnit.Fat.Value;
        if (macronutrientsTotal > MaximumMacronutrientsPerBaseUnit)
        {
            throw new ArgumentException(
                $"Protein, carbs and fat of an estimated food cannot add up to more than {MaximumMacronutrientsPerBaseUnit} g per {BaseQuantity}.",
                nameof(nutritionPerBaseUnit));
        }
    }
}
