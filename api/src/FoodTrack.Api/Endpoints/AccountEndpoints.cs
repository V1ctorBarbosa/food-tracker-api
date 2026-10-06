using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.Users;

namespace FoodTrack.Api.Endpoints;

public static class AccountEndpoints
{
    private const string RoutePrefix = "/account";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapDelete("/", DeleteAccountAsync).WithName(RouteNames.DeleteAccount);

        return app;
    }

    private static async Task<IResult> DeleteAccountAsync(
        ClaimsPrincipal user,
        AccountDeletionService accountDeletionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        await accountDeletionService.DeleteAccountAsync(userId, cancellationToken);

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string DeleteAccount = "DeleteAccount";
    }
}
