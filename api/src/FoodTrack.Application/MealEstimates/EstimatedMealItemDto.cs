using FoodTrack.Application.Meals;

namespace FoodTrack.Application.MealEstimates;

/// <summary>
/// An item ready to be saved: sent back as a meal item request with <see cref="Quantity"/>,
/// <see cref="Unit"/> and <see cref="EstimatedFood"/> unchanged.
/// </summary>
public sealed record EstimatedMealItemDto(
    double Quantity,
    string Unit,
    int Calories,
    double Protein,
    double Carbs,
    double Fat,
    EstimatedFoodDto EstimatedFood);
