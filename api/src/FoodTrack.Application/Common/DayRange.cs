namespace FoodTrack.Application.Common;

/// <summary>
/// An inclusive span of calendar days a caller asks history for. Bounded because every day in it
/// costs work on each request (database reads, one dashboard entry per day), so an unbounded span
/// lets a single request run until the function times out.
/// </summary>
public readonly record struct DayRange
{
    /// <summary>A full year, leap years included, so a year-long chart always fits.</summary>
    public const int MaximumDayCount = 366;

    public DateOnly From { get; }

    public DateOnly To { get; }

    /// <exception cref="ArgumentException">The range is reversed or longer than <see cref="MaximumDayCount"/> days.</exception>
    public DayRange(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("From cannot be after To.", nameof(from));
        }

        var dayCount = to.DayNumber - from.DayNumber + 1;
        if (dayCount > MaximumDayCount)
        {
            throw new ArgumentException($"A date range cannot span more than {MaximumDayCount} days.", nameof(to));
        }

        From = from;
        To = to;
    }
}
