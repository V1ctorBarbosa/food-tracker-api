using System.Security.Claims;
using FoodTrack.Domain.Users;

namespace FoodTrack.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the authenticated user id from the verified token's NameIdentifier claim.
    /// Endpoints must call this instead of trusting any user id present in a request body.
    /// </summary>
    public static UserId GetAuthenticatedUserId(this ClaimsPrincipal principal)
    {
        var uid = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated request is missing a user identifier claim.");

        return new UserId(uid);
    }
}
