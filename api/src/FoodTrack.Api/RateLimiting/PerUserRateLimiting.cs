using System.Security.Claims;
using System.Threading.RateLimiting;
using FoodTrack.Api.ErrorHandling;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodTrack.Api.RateLimiting;

/// <summary>
/// Caps how many requests each signed-in user can make per window, so one account cannot turn the
/// API into a stream of paid database reads and external lookups. Counters live in memory, so the
/// cap applies per running instance rather than across all of them; it bounds a single client's
/// burst, not the total load.
/// </summary>
public static class PerUserRateLimiting
{
    /// <summary>
    /// The policy for AI meal estimates, applied on top of the global limit. Every estimate is a
    /// paid model call, so its cap is far tighter than the one for ordinary requests.
    /// </summary>
    public const string MealEstimatesPolicyName = "MealEstimates";

    private const int PermitLimitPerWindow = 120;
    private const int MealEstimatePermitLimitPerWindow = 20;
    private const string RejectionMessage = "Too many requests. Please wait a moment and try again.";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MealEstimateWindow = TimeSpan.FromHours(1);

    public static IServiceCollection AddPerUserRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => PartitionFor(httpContext.User));
            options.AddPolicy(MealEstimatesPolicyName, httpContext => MealEstimatePartitionFor(httpContext.User));
            options.OnRejected = WriteRejectionAsync;
        });
    }

    /// <returns>The user id to count requests under, or null for a request without a signed-in user.</returns>
    public static string? PartitionKeyFor(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // Anonymous requests only ever reach the public health route: every other route is rejected by
    // authorization before this limiter runs. Sharing one bucket among them would let warm-up pings
    // from many devices throttle each other, so they are left unlimited.
    private static RateLimitPartition<string> PartitionFor(ClaimsPrincipal user)
    {
        var partitionKey = PartitionKeyFor(user);
        if (partitionKey is null)
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = PermitLimitPerWindow,
            Window = Window,
            QueueLimit = 0,
        });
    }

    private static RateLimitPartition<string> MealEstimatePartitionFor(ClaimsPrincipal user)
    {
        var partitionKey = PartitionKeyFor(user);
        if (partitionKey is null)
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = MealEstimatePermitLimitPerWindow,
            Window = MealEstimateWindow,
            QueueLimit = 0,
        });
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse(GeneralErrorCode.TooManyRequests, RejectionMessage),
            cancellationToken);
    }
}
