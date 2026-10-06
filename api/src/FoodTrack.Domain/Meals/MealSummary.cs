using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A meal without its items: when it happened, what kind it was and its nutrition totals. What
/// a day's list or the dashboard needs, so neither has to load every item of every meal.
/// </summary>
public sealed record MealSummary(
    MealId Id,
    DateTimeOffset LoggedAt,
    MealType MealType,
    Kilocalories TotalCalories,
    Grams TotalProtein,
    Grams TotalCarbs,
    Grams TotalFat);
