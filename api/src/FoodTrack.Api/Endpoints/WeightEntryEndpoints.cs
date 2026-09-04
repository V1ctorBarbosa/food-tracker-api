using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.WeightEntries;

namespace FoodTrack.Api.Endpoints;

public static class WeightEntryEndpoints
{
    private const string RoutePrefix = "/weight-entries";

    public static IEndpointRouteBuilder MapWeightEntryEndpoints(this IEndpointRouteBuilder app)
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
        WeightEntryService weightEntryService,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var entries = await weightEntryService.ListAsync(userId, from, to, cancellationToken);
        return Results.Ok(entries);
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        CreateWeightEntryRequest request,
        WeightEntryService weightEntryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.GetAuthenticatedUserId();
            var entry = await weightEntryService.CreateAsync(userId, request, cancellationToken);

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
        UpdateWeightEntryRequest request,
        WeightEntryService weightEntryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.GetAuthenticatedUserId();
            var entry = await weightEntryService.UpdateAsync(userId, new WeightEntryId(id), request, cancellationToken);

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
        WeightEntryService weightEntryService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await weightEntryService.DeleteAsync(userId, new WeightEntryId(id), cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string List = "ListWeightEntries";
        public const string Create = "CreateWeightEntry";
        public const string Update = "UpdateWeightEntry";
        public const string Delete = "DeleteWeightEntry";
    }
}
