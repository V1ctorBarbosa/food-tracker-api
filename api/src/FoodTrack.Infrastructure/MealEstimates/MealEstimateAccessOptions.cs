using System.ComponentModel.DataAnnotations;

namespace FoodTrack.Infrastructure.MealEstimates;

public sealed class MealEstimateAccessOptions
{
    public const string SectionName = "MealEstimates";

    /// <summary>
    /// Comma-separated user ids allowed to use AI estimates. A single string rather than a list,
    /// so it fits in one Parameter Store parameter. Empty means nobody.
    /// </summary>
    public string AllowedUserIds { get; set; } = string.Empty;

    /// <summary>
    /// Estimates allowed per calendar month (UTC) across every user. At about US$ 0.001 each, the
    /// default keeps a month well under the owner's R$ 10 budget even with re-runs.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MonthlyEstimateLimit { get; set; } = 300;
}
