namespace FoodTrack.Api.Authentication;

/// <summary>
/// What the identity provider currently says about a user's sessions: whether the account is
/// still active, and the moment before which every token issued to it was revoked (signing out of
/// all devices, a password change or an admin revocation all move it forward).
/// </summary>
public sealed record UserSessionStatus(bool IsAccountActive, DateTimeOffset? TokensValidAfter)
{
    /// <summary>The status of an account that was deleted or disabled: no token of it is accepted.</summary>
    public static UserSessionStatus Inactive { get; } = new(false, null);

    /// <returns>True when a token issued at <paramref name="tokenIssuedAt"/> must no longer be accepted.</returns>
    public bool Rejects(DateTimeOffset tokenIssuedAt)
    {
        if (!IsAccountActive)
        {
            return true;
        }

        if (TokensValidAfter is not { } tokensValidAfter)
        {
            return false;
        }

        return tokenIssuedAt < tokensValidAfter;
    }
}
