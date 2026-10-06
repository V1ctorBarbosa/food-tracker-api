namespace FoodTrack.Application.Meals;

/// <summary>A meal as it appears in a list: its totals, without the items.</summary>
public sealed record MealSummaryDto(
    string Id,
    DateTimeOffset LoggedAt,
    string MealType,
    int TotalCalories,
    double TotalProtein,
    double TotalCarbs,
    double TotalFat);
