namespace FoodTrack.Infrastructure.MealEstimates;

public sealed class MealEstimateAccessOptions
{
    public const string SectionName = "MealEstimates";

    /// <summary>
    /// Comma-separated user ids allowed to use AI estimates. A single string rather than a list,
    /// so it fits in one Parameter Store parameter. Empty means nobody.
    /// </summary>
    public string AllowedUserIds { get; set; } = string.Empty;
}
