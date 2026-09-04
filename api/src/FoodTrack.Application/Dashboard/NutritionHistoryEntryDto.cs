namespace FoodTrack.Application.Dashboard;

public sealed record NutritionHistoryEntryDto(
    DateOnly Date,
    int Calories,
    double Protein,
    double Carbs,
    double Fat,
    int WaterMilliliters);
