namespace FoodTrack.Application.Meals;

/// <summary>Exactly one of <see cref="FoodId"/> (catalog item) or <see cref="EstimatedFood"/> (AI-estimated item) is set.</summary>
public sealed record MealItemDto(
    string Id,
    string? FoodId,
    double Quantity,
    string Unit,
    int Calories,
    double Protein,
    double Carbs,
    double Fat,
    EstimatedFoodDto? EstimatedFood);
