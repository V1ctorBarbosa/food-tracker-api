namespace FoodTrack.Application.Meals;

public sealed record MealCaptureDto(string Id, bool HasPhoto, string? Description);
