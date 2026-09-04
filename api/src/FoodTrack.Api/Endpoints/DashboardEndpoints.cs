using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.Dashboard;

namespace FoodTrack.Api.Endpoints;

public static class DashboardEndpoints
{
    private const string RoutePrefix = "/dashboard";

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", GetDashboardAsync).WithName(RouteNames.GetDashboard);

        return app;
    }

    private static async Task<IResult> GetDashboardAsync(
        ClaimsPrincipal user,
        DashboardService dashboardService,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var dashboard = await dashboardService.GetDashboardAsync(userId, from, to, cancellationToken);

        if (dashboard is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(dashboard);
    }

    private static class RouteNames
    {
        public const string GetDashboard = "GetDashboard";
    }
}
