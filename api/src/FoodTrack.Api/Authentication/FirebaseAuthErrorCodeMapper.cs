using FoodTrack.Api.ErrorHandling;

namespace FoodTrack.Api.Authentication;

/// <summary>
/// Maps a Firebase Admin SDK <see cref="FirebaseAdmin.Auth.AuthErrorCode"/> to our own wire-level
/// <see cref="AuthErrorCode"/>. Only the two members we can act on differently are special-cased;
/// every other Firebase reason (malformed signature, wrong project, etc.) is bucketed as a
/// generic invalid-token failure.
/// </summary>
public static class FirebaseAuthErrorCodeMapper
{
    public static string Map(FirebaseAdmin.Auth.AuthErrorCode? firebaseAuthErrorCode) => firebaseAuthErrorCode switch
    {
        FirebaseAdmin.Auth.AuthErrorCode.ExpiredIdToken => AuthErrorCode.TokenExpired,
        FirebaseAdmin.Auth.AuthErrorCode.RevokedIdToken => AuthErrorCode.TokenRevoked,
        _ => AuthErrorCode.TokenInvalid,
    };
}
