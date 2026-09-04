namespace FoodTrack.Application.Meals;

public sealed record UpdateMealRequest(DateOnly Date, string MealType, IReadOnlyList<MealItemRequest> Items);
