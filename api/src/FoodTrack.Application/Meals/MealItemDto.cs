namespace FoodTrack.Application.Meals;

public sealed record MealItemDto(
    string Id,
    string FoodId,
    double Quantity,
    string Unit,
    int Calories,
    double Protein,
    double Carbs,
    double Fat);
