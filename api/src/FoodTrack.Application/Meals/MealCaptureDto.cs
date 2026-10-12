namespace FoodTrack.Application.Meals;

/// <param name="PhotoUrl">A short-lived URL to show the photo, or null when the capture has none.</param>
public sealed record MealCaptureDto(string Id, bool HasPhoto, string? Description, string? PhotoUrl);
