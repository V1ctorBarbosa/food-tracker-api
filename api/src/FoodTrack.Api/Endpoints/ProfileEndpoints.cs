using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.Users;

namespace FoodTrack.Api.Endpoints;

public static class ProfileEndpoints
{
    private const string RoutePrefix = "/profile";

    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", GetProfileAsync).WithName(RouteNames.GetProfile);
        group.MapPut("/", UpsertProfileAsync).WithName(RouteNames.UpsertProfile);

        return app;
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal user,
        UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var profile = await profileService.GetProfileAsync(userId, cancellationToken);

        if (profile is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> UpsertProfileAsync(
        ClaimsPrincipal user,
        UpsertUserProfileRequest request,
        UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.GetAuthenticatedUserId();
            var profile = await profileService.UpsertProfileAsync(userId, request, cancellationToken);

            return Results.Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static class RouteNames
    {
        public const string GetProfile = "GetProfile";
        public const string UpsertProfile = "UpsertProfile";
    }
}
