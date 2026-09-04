namespace FoodTrack.Api.ErrorHandling;

/// <summary>
/// The one response body shape for every error the API returns with a code -- both the
/// authentication challenge (401) and the global exception handler write this shape, so the
/// frontend has a single contract to parse regardless of where the error originated.
/// </summary>
public sealed record ApiErrorResponse(string ErrorCode, string Message);
