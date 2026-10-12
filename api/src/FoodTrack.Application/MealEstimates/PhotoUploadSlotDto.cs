namespace FoodTrack.Application.MealEstimates;

public sealed record PhotoUploadSlotDto(
    string CaptureId,
    string UploadUrl,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAt);
