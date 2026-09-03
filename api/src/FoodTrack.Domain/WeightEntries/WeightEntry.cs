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

    public DateOnly Date { get; }

    public Kilograms Weight { get; }

    private WeightEntry(WeightEntryId id, UserId userId, DateOnly date, Kilograms weight)
    {
        Id = id;
        UserId = userId;
        Date = date;
        Weight = weight;
    }

    public static WeightEntry Create(WeightEntryId id, UserId userId, DateOnly date, Kilograms weight) =>
        new(id, userId, date, weight);
}
