using FoodTrack.Application.Foods;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Api.Endpoints;

public static class FoodEndpoints
{
    private const string RoutePrefix = "/foods";

    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/{id}", GetByIdAsync).WithName(RouteNames.GetById);
        group.MapGet("/search", SearchAsync).WithName(RouteNames.Search);

        return app;
    }

    private static async Task<IResult> GetByIdAsync(string id, FoodService foodService, CancellationToken cancellationToken)
    {
        var food = await foodService.GetByIdAsync(new FoodId(id), cancellationToken);

        var getByIdResponse = food is null ? Results.NotFound() : Results.Ok(food);
        return getByIdResponse;
    }

    private static async Task<IResult> SearchAsync(string q, FoodService foodService, CancellationToken cancellationToken)
    {
        var foods = await foodService.SearchAsync(q, cancellationToken);
        return Results.Ok(foods);
    }

    private static class RouteNames
    {
        public const string GetById = "GetFoodById";
        public const string Search = "SearchFoods";
    }
}
