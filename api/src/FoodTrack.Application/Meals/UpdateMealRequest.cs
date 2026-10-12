namespace FoodTrack.Application.Meals;

/// <param name="Captures">
/// The meal's full list of captures. Omitted keeps the ones it already has, so a client that does
/// not know about captures can still edit a meal that has them.
/// </param>
public sealed record UpdateMealRequest(
    string MealType,
    IReadOnlyList<MealItemRequest> Items,
    IReadOnlyList<MealCaptureRequest>? Captures = null);
