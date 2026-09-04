using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Api.Endpoints;

public static class WaterEntryEndpoints
{
    private const string RoutePrefix = "/water-entries";

    public static IEndpointRouteBuilder MapWaterEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", ListAsync).WithName(RouteNames.List);
        group.MapPost("/", CreateAsync).WithName(RouteNames.Create);
        group.MapPut("/{id}", UpdateAsync).WithName(RouteNames.Update);
        group.MapDelete("/{id}", DeleteAsync).WithName(RouteNames.Delete);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal user,
        WaterEntryService waterEntryService,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var entries = await waterEntryService.ListAsync(userId, from, to, cancellationToken);
        return Results.Ok(entries);
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        CreateWaterEntryRequest request,
        WaterEntryService waterEntryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.GetAuthenticatedUserId();
            var entry = await waterEntryService.CreateAsync(userId, request, cancellationToken);

            var createdEntryLocation = $"{RoutePrefix}/{entry.Id}";
            return Results.Created(createdEntryLocation, entry);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateAsync(
        ClaimsPrincipal user,
        string id,
        UpdateWaterEntryRequest request,
        WaterEntryService waterEntryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.GetAuthenticatedUserId();
            var entry = await waterEntryService.UpdateAsync(userId, new WaterEntryId(id), request, cancellationToken);

            if (entry is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(entry);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAsync(
        ClaimsPrincipal user,
        string id,
        WaterEntryService waterEntryService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await waterEntryService.DeleteAsync(userId, new WaterEntryId(id), cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string List = "ListWaterEntries";
        public const string Create = "CreateWaterEntry";
        public const string Update = "UpdateWaterEntry";
        public const string Delete = "DeleteWaterEntry";
    }
}
