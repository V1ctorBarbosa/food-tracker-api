using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Foods;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrack.Api.Endpoints;

public static class FoodEndpoints
{
    private const string RoutePrefix = "/foods";

    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/{id}", GetByIdAsync).WithName(RouteNames.GetById);
        group.MapGet("/search", SearchAsync).WithName(RouteNames.Search);
        group.MapGet("/custom", ListCustomAsync).WithName(RouteNames.ListCustom);
        group.MapPost("/", CreateCustomAsync).WithName(RouteNames.CreateCustom);
        group.MapPut("/{id}", UpdateCustomAsync).WithName(RouteNames.UpdateCustom);

        return app;
    }

    private static async Task<IResult> ListCustomAsync(
        ClaimsPrincipal user,
        FoodService foodService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var foods = await foodService.ListCustomAsync(userId, cancellationToken);
        return Results.Ok(foods);
    }

    private static async Task<IResult> UpdateCustomAsync(
        ClaimsPrincipal user,
        string id,
        UpdateCustomFoodRequest request,
        FoodService foodService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var food = await foodService.UpdateCustomAsync(userId, new FoodId(id), request, cancellationToken);

        if (food is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(food);
    }

    private static async Task<IResult> GetByIdAsync(
        ClaimsPrincipal user,
        string id,
        FoodService foodService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var food = await foodService.GetByIdAsync(new FoodId(id), userId, cancellationToken);

        if (food is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(food);
    }

    private static async Task<IResult> SearchAsync(
        ClaimsPrincipal user,
        [FromQuery(Name = "q")] string searchQuery,
        FoodService foodService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var foods = await foodService.SearchAsync(userId, searchQuery, cancellationToken);
        return Results.Ok(foods);
    }

    private static async Task<IResult> CreateCustomAsync(
        ClaimsPrincipal user,
        CreateCustomFoodRequest request,
        FoodService foodService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var food = await foodService.CreateCustomAsync(userId, request, cancellationToken);

        var createdFoodLocation = $"{RoutePrefix}/{food.Id}";
        return Results.Created(createdFoodLocation, food);
    }

    private static class RouteNames
    {
        public const string GetById = "GetFoodById";
        public const string Search = "SearchFoods";
        public const string CreateCustom = "CreateCustomFood";
        public const string ListCustom = "ListCustomFoods";
        public const string UpdateCustom = "UpdateCustomFood";
    }
}
