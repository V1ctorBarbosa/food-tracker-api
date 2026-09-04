namespace FoodTrack.Application.Meals;

public sealed record CreateMealRequest(DateOnly Date, string MealType, IReadOnlyList<MealItemRequest> Items);
