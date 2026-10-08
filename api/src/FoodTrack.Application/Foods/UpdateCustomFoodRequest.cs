namespace FoodTrack.Application.Foods;

/// <summary>Always per 100g, like <see cref="CreateCustomFoodRequest"/>: only the name and the nutrition can change.</summary>
public sealed record UpdateCustomFoodRequest(string Name, int Calories, double Protein, double Carbs, double Fat);
