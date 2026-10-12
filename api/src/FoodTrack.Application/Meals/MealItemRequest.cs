namespace FoodTrack.Application.Meals;

/// <summary>
/// One item to log within a meal, used for both creating and replacing a meal. Send exactly one of
/// <see cref="FoodId"/> (a catalog food) or <see cref="EstimatedFood"/> (an AI estimate).
/// <see cref="Unit"/> must measure what the food is measured by: Gram or Kilogram for a food,
/// Milliliter or Liter for a drink.
/// </summary>
public sealed record MealItemRequest(string? FoodId, double Quantity, string Unit, EstimatedFoodRequest? EstimatedFood = null);
