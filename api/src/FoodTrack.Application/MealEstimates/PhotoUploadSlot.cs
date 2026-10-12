namespace FoodTrack.Application.MealEstimates;

/// <param name="Headers">Headers the upload must send exactly as given, or the storage rejects it.</param>
public sealed record PhotoUploadSlot(Uri UploadUrl, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);
