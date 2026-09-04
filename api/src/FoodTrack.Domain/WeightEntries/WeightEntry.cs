using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Domain.WeightEntries;

/// <summary>
/// A historical weight record. The user does not need to log weight every day —
/// progression is derived from the history of entries, never persisted separately.
/// </summary>
public sealed class WeightEntry
{
    public WeightEntryId Id { get; }

    public UserId UserId { get; }

    public DateTimeOffset LoggedAt { get; }

    public Kilograms Weight { get; }

    private WeightEntry(WeightEntryId id, UserId userId, DateTimeOffset loggedAt, Kilograms weight)
    {
        Id = id;
        UserId = userId;
        LoggedAt = loggedAt;
        Weight = weight;
    }

    public static WeightEntry Create(WeightEntryId id, UserId userId, DateTimeOffset loggedAt, Kilograms weight) =>
        new(id, userId, loggedAt, weight);
}
