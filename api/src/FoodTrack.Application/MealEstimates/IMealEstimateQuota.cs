namespace FoodTrack.Application.MealEstimates;

/// <summary>
/// Caps how many estimates run in a calendar month (UTC), counted across every user, because every
/// estimate is paid by the app owner. It complements the per-user hourly rate limit, which only
/// spaces requests out, and the spend limit on the Anthropic account, which is the hard cap.
/// </summary>
public interface IMealEstimateQuota
{
    /// <summary>Counts one estimate for the month of <paramref name="now"/>, unless the month's cap is already reached.</summary>
    /// <returns>False when the cap is reached; nothing is counted then.</returns>
    Task<bool> TryConsumeAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
