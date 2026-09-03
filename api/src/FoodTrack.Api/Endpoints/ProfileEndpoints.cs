using System.Security.Claims;
using FoodTrack.Application.Users;
using FoodTrack.Domain.Users;

namespace FoodTrack.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/profile", GetProfileAsync)
            .RequireAuthorization()
            .WithName("GetProfile");

        app.MapPut("/profile", UpsertProfileAsync)
            .RequireAuthorization()
            .WithName("UpsertProfile");

        return app;
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal user,
        UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId(user);
        var profile = await profileService.GetProfileAsync(userId, cancellationToken);

        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> UpsertProfileAsync(
        ClaimsPrincipal user,
        UpsertUserProfileRequest request,
        UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId(user);
            var profile = await profileService.UpsertProfileAsync(userId, request, cancellationToken);

            return Results.Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static UserId GetAuthenticatedUserId(ClaimsPrincipal principal)
    {
        var uid = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated request is missing a user identifier claim.");

        return new UserId(uid);
    }
}
