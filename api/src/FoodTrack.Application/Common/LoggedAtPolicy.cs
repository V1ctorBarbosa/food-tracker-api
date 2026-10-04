namespace FoodTrack.Application.Common;

/// <summary>
/// Decides when an entry is logged. A client may back-date an entry to a day it forgot to fill in,
/// but may not log something in the future.
/// </summary>
public static class LoggedAtPolicy
{
    // Clients and the server clock drift a little; a few minutes ahead is not a future entry.
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    /// <returns><paramref name="now"/> when no time was requested, otherwise the requested time.</returns>
    /// <exception cref="ArgumentException">The requested time is further ahead than the tolerance allows.</exception>
    public static DateTimeOffset Resolve(DateTimeOffset? requestedLoggedAt, DateTimeOffset now)
    {
        if (requestedLoggedAt is null)
        {
            return now;
        }

        if (requestedLoggedAt.Value > now + FutureTolerance)
        {
            throw new ArgumentException("LoggedAt cannot be in the future.", nameof(requestedLoggedAt));
        }

        return requestedLoggedAt.Value;
    }
}
