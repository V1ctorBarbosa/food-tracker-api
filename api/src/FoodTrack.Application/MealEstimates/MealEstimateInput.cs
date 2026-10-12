using FoodTrack.Domain.Meals;

namespace FoodTrack.Application.MealEstimates;

/// <param name="PhotoUrl">A short-lived URL the estimator can fetch the photo from, or null for a description-only estimate.</param>
public sealed record MealEstimateInput(Uri? PhotoUrl, MealDescription? Description);
