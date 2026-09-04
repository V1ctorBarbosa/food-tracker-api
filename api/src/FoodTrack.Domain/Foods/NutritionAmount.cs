using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Foods;

/// <summary>The nutritional values for a specific quantity of a <see cref="Food"/>.</summary>
public readonly record struct NutritionAmount(Kilocalories Calories, Grams Protein, Grams Carbs, Grams Fat);
