using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FoodTrack.Api.Authentication;

/// <summary>
/// Verifies the Firebase ID token on the Authorization header and builds a ClaimsPrincipal
/// from it. The authenticated user id always comes from the verified token's uid claim —
/// endpoints must never read a user id from the request body.
/// </summary>
public sealed class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Firebase";

    private const string BearerPrefix = "Bearer ";

    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        FirebaseApp firebaseApp)
        : base(options, logger, encoder)
    {
        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var headerValue = authorizationHeader.ToString();
        if (!headerValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var idToken = headerValue[BearerPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(idToken))
        {
            return AuthenticateResult.Fail("Missing bearer token.");
        }

        try
        {
            var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken);

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, decodedToken.Uid) };
            if (decodedToken.Claims.TryGetValue("email", out var email) && email is not null)
            {
                claims.Add(new Claim(ClaimTypes.Email, email.ToString()!));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException ex)
        {
            Logger.LogWarning(ex, "Firebase ID token verification failed.");
            return AuthenticateResult.Fail("Invalid or expired token.");
        }
    }
}
