using FoodTrack.Application.Common;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;

namespace FoodTrack.Application.WeightEntries;

public sealed class WeightEntryService
{
    private readonly IWeightEntryRepository _weightEntryRepository;

    public WeightEntryService(IWeightEntryRepository weightEntryRepository)
    {
        _weightEntryRepository = weightEntryRepository;
    }

    public async Task<IReadOnlyList<WeightEntryDto>> ListAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        var entries = await _weightEntryRepository.ListByUserAsync(userId, from, to, utcOffset, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    public async Task<WeightEntryDto> CreateAsync(
        UserId userId,
        CreateWeightEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var loggedAt = LoggedAtPolicy.Resolve(request.LoggedAt, DateTimeOffset.UtcNow);
        var entry = WeightEntry.Create(WeightEntryId.NewId(), userId, loggedAt, new Kilograms(request.WeightKilograms));
        await _weightEntryRepository.AddAsync(entry, cancellationToken);
        return ToDto(entry);
    }

    /// <returns>The updated entry, or null when it does not exist or does not belong to <paramref name="userId"/>.</returns>
    public async Task<WeightEntryDto?> UpdateAsync(
        UserId userId,
        WeightEntryId id,
        UpdateWeightEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _weightEntryRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = WeightEntry.Create(id, userId, existing.LoggedAt, new Kilograms(request.WeightKilograms));
        await _weightEntryRepository.UpdateAsync(updated, cancellationToken);
        return ToDto(updated);
    }

    /// <returns>True when the entry existed and belonged to <paramref name="userId"/> and was deleted.</returns>
    public async Task<bool> DeleteAsync(UserId userId, WeightEntryId id, CancellationToken cancellationToken = default)
    {
        var existing = await _weightEntryRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await _weightEntryRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static WeightEntryDto ToDto(WeightEntry entry) => new(entry.Id.Value, entry.LoggedAt, entry.Weight.Value);
}
