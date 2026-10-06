using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FoodTrack.Api.ErrorHandling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using AuthErrorCode = FoodTrack.Api.ErrorHandling.AuthErrorCode;

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
    private readonly IUserSessionStatusProvider _userSessionStatusProvider;

    private string _authErrorCode = AuthErrorCode.MissingCredentials;

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        FirebaseApp firebaseApp,
        IUserSessionStatusProvider userSessionStatusProvider)
        : base(options, logger, encoder)
    {
        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
        _userSessionStatusProvider = userSessionStatusProvider;
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

        FirebaseToken decodedToken;
        try
        {
            decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken, Context.RequestAborted);
        }
        catch (FirebaseAuthException ex)
        {
            Logger.LogWarning(ex, "Firebase ID token verification failed.");
            _authErrorCode = FirebaseAuthErrorCodeMapper.Map(ex.AuthErrorCode);
            return AuthenticateResult.Fail("Invalid or expired token.");
        }

        // Outside the try above on purpose: failing to reach the identity provider is a server
        // error, not a reason to tell the client its credentials are bad and sign it out.
        var sessionStatus = await _userSessionStatusProvider.GetAsync(decodedToken.Uid, Context.RequestAborted);
        var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(decodedToken.IssuedAtTimeSeconds);
        if (sessionStatus.Rejects(tokenIssuedAt))
        {
            _authErrorCode = AuthErrorCode.TokenRevoked;
            return AuthenticateResult.Fail("The token was revoked or the account is no longer active.");
        }

        return AuthenticateResult.Success(BuildTicket(decodedToken));
    }

    private static AuthenticationTicket BuildTicket(FirebaseToken decodedToken)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, decodedToken.Uid) };
        if (decodedToken.Claims.TryGetValue("email", out var email) && email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email.ToString()!));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, SchemeName);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";

        return Response.WriteAsJsonAsync(new ApiErrorResponse(_authErrorCode, BuildMessage(_authErrorCode)));
    }

    private static string BuildMessage(string authErrorCode) => authErrorCode switch
    {
        AuthErrorCode.TokenExpired => "The session token has expired.",
        AuthErrorCode.TokenRevoked => "The session token was revoked.",
        AuthErrorCode.TokenInvalid => "The provided credentials are invalid.",
        _ => "No credentials were provided.",
    };
}
