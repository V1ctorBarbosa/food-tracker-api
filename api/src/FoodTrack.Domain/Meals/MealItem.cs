using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A food consumed within a <see cref="Meal"/> (docs section 11). Stores a nutritional
/// snapshot (docs section 12): the calories/protein/carbs/fat computed from the Food at the
/// moment it was logged, not a live link to it -- if the Food's data is corrected later,
/// this MealItem still reflects what was actually eaten.
/// </summary>
public sealed class MealItem
{
    public MealItemId Id { get; }

    public FoodId FoodId { get; }

    public double Quantity { get; }

    public MeasurementUnit Unit { get; }

    public Kilocalories Calories { get; }

    public Grams Protein { get; }

    public Grams Carbs { get; }

    public Grams Fat { get; }

    private MealItem(
        MealItemId id,
        FoodId foodId,
        double quantity,
        MeasurementUnit unit,
        Kilocalories calories,
        Grams protein,
        Grams carbs,
        Grams fat)
    {
        Id = id;
        FoodId = foodId;
        Quantity = quantity;
        Unit = unit;
        Calories = calories;
        Protein = protein;
        Carbs = carbs;
        Fat = fat;
    }

    public static MealItem Create(
        MealItemId id,
        FoodId foodId,
        double quantity,
        MeasurementUnit unit,
        Kilocalories calories,
        Grams protein,
        Grams carbs,
        Grams fat)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
        }

        return new MealItem(id, foodId, quantity, unit, calories, protein, carbs, fat);
    }
}
