namespace FoodTrack.Api.ErrorHandling;

/// <summary>
/// Wire-level error codes for authentication/identity failures, distinguishing "never
/// presented credentials" from "presented credentials that are no longer valid" so the
/// frontend can react and message each case differently.
/// </summary>
public static class AuthErrorCode
{
    public const string MissingCredentials = "MissingCredentials";
    public const string TokenExpired = "TokenExpired";
    public const string TokenRevoked = "TokenRevoked";
    public const string TokenInvalid = "TokenInvalid";
}
