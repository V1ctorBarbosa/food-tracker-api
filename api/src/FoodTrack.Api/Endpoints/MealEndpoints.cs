using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Api.Endpoints;

public static class MealEndpoints
{
    private const string RoutePrefix = "/meals";

    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", ListAsync).WithName(RouteNames.List);
        group.MapGet("/{id}", GetByIdAsync).WithName(RouteNames.GetById);
        group.MapPost("/", CreateAsync).WithName(RouteNames.Create);
        group.MapPut("/{id}", UpdateAsync).WithName(RouteNames.Update);
        group.MapDelete("/{id}", DeleteAsync).WithName(RouteNames.Delete);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal user,
        MealService mealService,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var meals = await mealService.ListAsync(userId, from, to, cancellationToken);
        return Results.Ok(meals);
    }

    private static async Task<IResult> GetByIdAsync(
        ClaimsPrincipal user,
        string id,
        MealService mealService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var meal = await mealService.GetByIdAsync(userId, new MealId(id), cancellationToken);

        if (meal is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(meal);
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal user,
        CreateMealRequest request,
        MealService mealService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var meal = await mealService.CreateAsync(userId, request, cancellationToken);

        var createdMealLocation = $"{RoutePrefix}/{meal.Id}";
        return Results.Created(createdMealLocation, meal);
    }

    private static async Task<IResult> UpdateAsync(
        ClaimsPrincipal user,
        string id,
        UpdateMealRequest request,
        MealService mealService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var meal = await mealService.UpdateAsync(userId, new MealId(id), request, cancellationToken);

        if (meal is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(meal);
    }

    private static async Task<IResult> DeleteAsync(
        ClaimsPrincipal user,
        string id,
        MealService mealService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await mealService.DeleteAsync(userId, new MealId(id), cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string List = "ListMeals";
        public const string GetById = "GetMealById";
        public const string Create = "CreateMeal";
        public const string Update = "UpdateMeal";
        public const string Delete = "DeleteMeal";
    }
}
