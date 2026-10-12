using FoodTrack.Application.MealEstimates;
using FoodTrack.Domain.Users;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure.MealEstimates;

public sealed class ConfigurationMealEstimateAccessPolicy : IMealEstimateAccessPolicy
{
    private const char UserIdSeparator = ',';

    private readonly IReadOnlySet<string> _allowedUserIds;

    public ConfigurationMealEstimateAccessPolicy(IOptions<MealEstimateAccessOptions> options)
    {
        _allowedUserIds = ParseAllowedUserIds(options.Value.AllowedUserIds);
    }

    public bool CanEstimate(UserId userId) => _allowedUserIds.Contains(userId.Value);

    public static IReadOnlySet<string> ParseAllowedUserIds(string allowedUserIds) =>
        allowedUserIds
            .Split(UserIdSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
}
