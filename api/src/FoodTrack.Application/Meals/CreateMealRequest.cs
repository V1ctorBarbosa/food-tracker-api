namespace FoodTrack.Application.Meals;

/// <param name="Captures">The estimates the meal's AI items came from. Omitted means none.</param>
public sealed record CreateMealRequest(
    string MealType,
    IReadOnlyList<MealItemRequest> Items,
    DateTimeOffset? LoggedAt = null,
    IReadOnlyList<MealCaptureRequest>? Captures = null);
