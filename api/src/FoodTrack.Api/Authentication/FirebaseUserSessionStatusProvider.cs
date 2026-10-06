using FirebaseAdmin;
using FirebaseAdmin.Auth;

namespace FoodTrack.Api.Authentication;

/// <summary>Reads a user's session status from their Firebase Auth user record.</summary>
public sealed class FirebaseUserSessionStatusProvider : IUserSessionStatusProvider
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseUserSessionStatusProvider(FirebaseApp firebaseApp)
    {
        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
    }

    public async Task<UserSessionStatus> GetAsync(string uid, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRecord = await _firebaseAuth.GetUserAsync(uid, cancellationToken);
            return ToSessionStatus(userRecord.Disabled, userRecord.TokensValidAfterTimestamp);
        }
        catch (FirebaseAuthException exception) when (exception.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return UserSessionStatus.Inactive;
        }
    }

    /// <summary>
    /// Firebase reports "never revoked" as <see cref="DateTime.MinValue"/> rather than null, and
    /// its timestamps are UTC.
    /// </summary>
    public static UserSessionStatus ToSessionStatus(bool isDisabled, DateTime tokensValidAfterUtc)
    {
        if (isDisabled)
        {
            return UserSessionStatus.Inactive;
        }

        if (tokensValidAfterUtc == DateTime.MinValue)
        {
            return new UserSessionStatus(true, null);
        }

        var tokensValidAfter = new DateTimeOffset(DateTime.SpecifyKind(tokensValidAfterUtc, DateTimeKind.Utc));
        return new UserSessionStatus(true, tokensValidAfter);
    }
}
