namespace FoodTrack.Application.Common;

/// <summary>
/// Turns calendar days into instants in the user's own time zone, so "today" means the user's
/// today and not UTC's. An entry logged at 22:00 in São Paulo belongs to that evening's date,
/// even though it is already the next day in UTC.
/// </summary>
public static class LocalDayRange
{
    /// <returns>The instant the first day starts (inclusive) and the instant after the last day ends (exclusive).</returns>
    public static (DateTimeOffset FromInclusive, DateTimeOffset ToExclusive) ToInstants(DateOnly from, DateOnly to, TimeSpan utcOffset)
    {
        var fromInclusive = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), utcOffset);
        var toExclusive = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), utcOffset);

        return (fromInclusive, toExclusive);
    }

    /// <returns>The calendar day <paramref name="instant"/> falls on for someone at <paramref name="utcOffset"/>.</returns>
    public static DateOnly DateOf(DateTimeOffset instant, TimeSpan utcOffset)
    {
        return DateOnly.FromDateTime(instant.ToOffset(utcOffset).DateTime);
    }
}
