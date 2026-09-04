namespace FoodTrack.Application.Foods;

/// <summary>Always per 100g -- matches every other <see cref="FoodDto"/> base unit convention.</summary>
public sealed record CreateCustomFoodRequest(string Name, int Calories, double Protein, double Carbs, double Fat);
