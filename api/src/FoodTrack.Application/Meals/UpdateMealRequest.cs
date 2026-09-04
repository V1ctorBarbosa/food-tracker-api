namespace FoodTrack.Application.Meals;

public sealed record UpdateMealRequest(string MealType, IReadOnlyList<MealItemRequest> Items);
