using FoodTrack.Application.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;

namespace FoodTrack.Application.WeightEntries;

public interface IWeightEntryRepository
{
    /// <summary>
    /// Looks up an entry scoped to its owner. Returns null both when the id does not exist
    /// and when it belongs to a different user, so callers never learn which case it was.
    /// </summary>
    Task<WeightEntry?> GetByIdAsync(WeightEntryId id, UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeightEntry>> ListByUserAsync(
        UserId userId,
        DayRange dayRange,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The single most recent entry for the user, regardless of date range -- used to answer
    /// "what does the user weigh right now" independently of any chart's date window.
    /// </summary>
    Task<WeightEntry?> GetMostRecentAsync(UserId userId, CancellationToken cancellationToken = default);

    Task AddAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WeightEntryId id, CancellationToken cancellationToken = default);
}
