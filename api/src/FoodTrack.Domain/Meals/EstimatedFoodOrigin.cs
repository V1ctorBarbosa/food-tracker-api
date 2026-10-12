namespace FoodTrack.Domain.Meals;

/// <summary>The item was estimated by the AI from the <see cref="MealCapture"/> identified by <see cref="CaptureId"/>.</summary>
public sealed record EstimatedFoodOrigin(EstimatedFood Food, CaptureId CaptureId) : MealItemOrigin;
