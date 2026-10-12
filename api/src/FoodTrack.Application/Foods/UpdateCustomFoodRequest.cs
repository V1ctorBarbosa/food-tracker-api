namespace FoodTrack.Application.Foods;

/// <summary>
/// Nutrition is per 100 of <see cref="BaseUnitType"/>: "Gram" for a food, "Milliliter" for a drink.
/// Left out, the food keeps the unit it has.
/// </summary>
public sealed record UpdateCustomFoodRequest(
    string Name,
    int Calories,
    double Protein,
    double Carbs,
    double Fat,
    string? BaseUnitType = null);
