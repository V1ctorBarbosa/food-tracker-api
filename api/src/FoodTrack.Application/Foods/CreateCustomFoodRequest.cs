namespace FoodTrack.Application.Foods;

/// <summary>
/// Nutrition is per 100 of <see cref="BaseUnitType"/>: "Gram" for a food, "Milliliter" for a drink.
/// Left out, it is "Gram".
/// </summary>
public sealed record CreateCustomFoodRequest(
    string Name,
    int Calories,
    double Protein,
    double Carbs,
    double Fat,
    string? BaseUnitType = null);
