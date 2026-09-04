namespace FoodTrack.Application.Meals;

/// <summary>
/// One item to log within a meal. Provide either (<see cref="Quantity"/> + <see cref="Unit"/>)
/// for a raw amount, or (<see cref="FoodServingId"/> + <see cref="ServingCount"/>) to log by a
/// named serving (e.g. "1 can") -- not both. Used for both creating and replacing a meal.
/// </summary>
public sealed record MealItemRequest(
    string FoodId,
    double? Quantity,
    string? Unit,
    string? FoodServingId,
    double? ServingCount);
