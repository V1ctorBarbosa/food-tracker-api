using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A food consumed within a <see cref="Meal"/> (docs section 11). Stores a nutritional
/// snapshot (docs section 12): the calories/protein/carbs/fat computed at the moment it was
/// logged, not a live link -- if a catalog Food's data is corrected later, this MealItem still
/// reflects what was actually eaten. <see cref="Origin"/> says where those values came from.
/// </summary>
public sealed class MealItem
{
    /// <summary>
    /// Caps an AI-estimated quantity, so a misread photo or a typo cannot log a meal of several
    /// kilos. Catalog items are bounded by their own nutrition values instead.
    /// </summary>
    public const double MaximumEstimatedQuantity = 2_000;

    public MealItemId Id { get; }

    public MealItemOrigin Origin { get; }

    public double Quantity { get; }

    public MeasurementUnit Unit { get; }

    public Kilocalories Calories { get; }

    public Grams Protein { get; }

    public Grams Carbs { get; }

    public Grams Fat { get; }

    private MealItem(
        MealItemId id,
        MealItemOrigin origin,
        double quantity,
        MeasurementUnit unit,
        Kilocalories calories,
        Grams protein,
        Grams carbs,
        Grams fat)
    {
        Id = id;
        Origin = origin;
        Quantity = quantity;
        Unit = unit;
        Calories = calories;
        Protein = protein;
        Carbs = carbs;
        Fat = fat;
    }

    /// <summary>Rebuilds an item from a stored snapshot, without recalculating it.</summary>
    public static MealItem Create(
        MealItemId id,
        MealItemOrigin origin,
        double quantity,
        MeasurementUnit unit,
        Kilocalories calories,
        Grams protein,
        Grams carbs,
        Grams fat)
    {
        EnsureQuantityIsPositive(quantity);

        return new MealItem(id, origin, quantity, unit, calories, protein, carbs, fat);
    }

    /// <summary>Rebuilds an item logged from a catalog food, without recalculating it.</summary>
    public static MealItem Create(
        MealItemId id,
        FoodId foodId,
        double quantity,
        MeasurementUnit unit,
        Kilocalories calories,
        Grams protein,
        Grams carbs,
        Grams fat) =>
        Create(id, new CatalogFoodOrigin(foodId), quantity, unit, calories, protein, carbs, fat);

    /// <summary>Logs <paramref name="quantity"/> of a catalog food, taking the snapshot from its current values.</summary>
    /// <exception cref="ArgumentException">The quantity is not positive, or the unit does not measure the food.</exception>
    public static MealItem FromCatalogFood(MealItemId id, Food food, double quantity, MeasurementUnit unit)
    {
        EnsureQuantityIsPositive(quantity);
        var nutrition = NutritionCalculator.Calculate(food, quantity, unit);

        return FromNutrition(id, new CatalogFoodOrigin(food.Id), quantity, unit, nutrition);
    }

    /// <summary>Logs <paramref name="quantity"/> of an AI-estimated food, taking the snapshot from its values per base unit.</summary>
    /// <exception cref="ArgumentException">
    /// The quantity is not positive or above <see cref="MaximumEstimatedQuantity"/>, or the unit does not measure the food.
    /// </exception>
    public static MealItem FromEstimatedFood(
        MealItemId id,
        EstimatedFood estimatedFood,
        CaptureId captureId,
        double quantity,
        MeasurementUnit unit)
    {
        EnsureQuantityIsPositive(quantity);
        EnsureEstimatedQuantityIsAllowed(quantity);
        var nutrition = estimatedFood.CalculateFor(quantity, unit);

        return FromNutrition(id, new EstimatedFoodOrigin(estimatedFood, captureId), quantity, unit, nutrition);
    }

    private static MealItem FromNutrition(
        MealItemId id,
        MealItemOrigin origin,
        double quantity,
        MeasurementUnit unit,
        NutritionAmount nutrition) =>
        new(id, origin, quantity, unit, nutrition.Calories, nutrition.Protein, nutrition.Carbs, nutrition.Fat);

    private static void EnsureQuantityIsPositive(double quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
        }
    }

    private static void EnsureEstimatedQuantityIsAllowed(double quantity)
    {
        if (quantity > MaximumEstimatedQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                $"An estimated quantity cannot be above {MaximumEstimatedQuantity}.");
        }
    }
}
