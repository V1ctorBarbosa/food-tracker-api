using System.Security.Claims;
using FoodTrack.Api.Authentication;
using FoodTrack.Api.RateLimiting;
using FoodTrack.Application.MealEstimates;

namespace FoodTrack.Api.Endpoints;

public static class MealEstimateEndpoints
{
    private const string RoutePrefix = "/meal-estimates";

    public static IEndpointRouteBuilder MapMealEstimateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix)
            .RequireAuthorization()
            .RequireRateLimiting(PerUserRateLimiting.MealEstimatesPolicyName);

        group.MapPost("/upload-slots", CreateUploadSlotAsync).WithName(RouteNames.CreateUploadSlot);
        group.MapPost("/", EstimateAsync).WithName(RouteNames.Estimate);

        return app;
    }

    private static async Task<IResult> CreateUploadSlotAsync(
        ClaimsPrincipal user,
        MealEstimateService mealEstimateService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var uploadSlot = await mealEstimateService.CreateUploadSlotAsync(userId, cancellationToken);

        return Results.Ok(uploadSlot);
    }

    private static async Task<IResult> EstimateAsync(
        ClaimsPrincipal user,
        EstimateMealRequest request,
        MealEstimateService mealEstimateService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var estimate = await mealEstimateService.EstimateAsync(userId, request, cancellationToken);

        return Results.Ok(estimate);
    }

    private static class RouteNames
    {
        public const string CreateUploadSlot = "CreateMealPhotoUploadSlot";
        public const string Estimate = "EstimateMeal";
    }
}
