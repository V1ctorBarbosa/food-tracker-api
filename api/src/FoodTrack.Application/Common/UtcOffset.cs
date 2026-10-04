namespace FoodTrack.Application.Common;

/// <summary>Reads the client's UTC offset, sent as minutes (e.g. -180 for São Paulo).</summary>
public static class UtcOffset
{
    private const int MinimumMinutes = -12 * 60;
    private const int MaximumMinutes = 14 * 60;

    /// <exception cref="ArgumentException">The offset is outside the range real time zones use.</exception>
    public static TimeSpan FromMinutes(int minutes)
    {
        if (minutes < MinimumMinutes || minutes > MaximumMinutes)
        {
            throw new ArgumentException(
                $"UtcOffsetMinutes must be between {MinimumMinutes} and {MaximumMinutes}.",
                nameof(minutes));
        }

        return TimeSpan.FromMinutes(minutes);
    }
}
