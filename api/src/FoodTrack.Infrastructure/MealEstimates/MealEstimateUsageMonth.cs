using System.Globalization;

namespace FoodTrack.Infrastructure.MealEstimates;

/// <summary>The pure rules behind the monthly estimate cap, kept apart from the Firestore transaction.</summary>
internal static class MealEstimateUsageMonth
{
    private const string MonthKeyFormat = "yyyy-MM";

    /// <summary>"2026-10": the calendar month of <paramref name="instant"/> in UTC, so every instance and user agrees on it.</summary>
    public static string KeyFor(DateTimeOffset instant) =>
        instant.UtcDateTime.ToString(MonthKeyFormat, CultureInfo.InvariantCulture);

    public static bool HasRoomFor(int usedCount, int monthlyLimit) => usedCount < monthlyLimit;
}
