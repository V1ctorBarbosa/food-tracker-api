namespace FoodTrack.Application.Meals;

public sealed record CreateMealRequest(string MealType, IReadOnlyList<MealItemRequest> Items, DateTimeOffset? LoggedAt = null);
