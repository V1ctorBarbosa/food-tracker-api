using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Domain.WaterEntries;

/// <summary>
/// A single water intake log. Unlike a weight measurement, water is naturally logged as
/// several small entries throughout the day; the daily total is derived by summing them,
/// never persisted on its own.
/// </summary>
public sealed class WaterEntry
{
    public WaterEntryId Id { get; }

    public UserId UserId { get; }

    public DateTimeOffset LoggedAt { get; }

    public Milliliters Amount { get; }

    private WaterEntry(WaterEntryId id, UserId userId, DateTimeOffset loggedAt, Milliliters amount)
    {
        Id = id;
        UserId = userId;
        LoggedAt = loggedAt;
        Amount = amount;
    }

    public static WaterEntry Create(WaterEntryId id, UserId userId, DateTimeOffset loggedAt, Milliliters amount) =>
        new(id, userId, loggedAt, amount);
}
