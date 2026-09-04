namespace FoodTrack.Application.Meals;

public sealed record MealDto(
    string Id,
    DateOnly Date,
    string MealType,
    int TotalCalories,
    double TotalProtein,
    double TotalCarbs,
    double TotalFat,
    IReadOnlyList<MealItemDto> Items);
