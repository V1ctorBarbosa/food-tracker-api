namespace FoodTrack.Application.Meals;

/// <summary>
/// One item to log within a meal, used for both creating and replacing a meal. <see cref="Unit"/>
/// must measure what the food is measured by: Gram or Kilogram for a food, Milliliter or Liter for a drink.
/// </summary>
public sealed record MealItemRequest(string FoodId, double Quantity, string Unit);
