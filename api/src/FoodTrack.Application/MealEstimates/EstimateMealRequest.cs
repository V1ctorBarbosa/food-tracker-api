namespace FoodTrack.Application.MealEstimates;

/// <param name="CaptureId">The upload slot's capture for a photo estimate; omitted for a description-only one.</param>
public sealed record EstimateMealRequest(string? CaptureId, string? Description);
