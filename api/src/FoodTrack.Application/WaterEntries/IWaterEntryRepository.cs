using FoodTrack.Application.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Application.WaterEntries;

public interface IWaterEntryRepository
{
    /// <summary>
    /// Looks up an entry scoped to its owner. Returns null both when the id does not exist
    /// and when it belongs to a different user, so callers never learn which case it was.
    /// </summary>
    Task<WaterEntry?> GetByIdAsync(WaterEntryId id, UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaterEntry>> ListByUserAsync(
        UserId userId,
        DayRange dayRange,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default);

    Task AddAsync(WaterEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WaterEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WaterEntryId id, CancellationToken cancellationToken = default);

    Task DeleteAllByUserAsync(UserId userId, CancellationToken cancellationToken = default);
}
