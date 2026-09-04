namespace FoodTrack.Application.Meals;

public sealed record MealDto(
    string Id,
    DateTimeOffset LoggedAt,
    string MealType,
    int TotalCalories,
    double TotalProtein,
    double TotalCarbs,
    double TotalFat,
    IReadOnlyList<MealItemDto> Items);
