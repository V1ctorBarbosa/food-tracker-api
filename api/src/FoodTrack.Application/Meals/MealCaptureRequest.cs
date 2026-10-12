namespace FoodTrack.Application.Meals;

public sealed record MealCaptureRequest(string Id, bool HasPhoto, string? Description);
