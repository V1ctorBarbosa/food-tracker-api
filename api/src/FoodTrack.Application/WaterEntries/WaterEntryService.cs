using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Application.WaterEntries;

public sealed class WaterEntryService
{
    private readonly IWaterEntryRepository _waterEntryRepository;

    public WaterEntryService(IWaterEntryRepository waterEntryRepository)
    {
        _waterEntryRepository = waterEntryRepository;
    }

    public async Task<IReadOnlyList<WaterEntryDto>> ListAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var entries = await _waterEntryRepository.ListByUserAsync(userId, from, to, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    public async Task<WaterEntryDto> CreateAsync(
        UserId userId,
        CreateWaterEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = WaterEntry.Create(WaterEntryId.NewId(), userId, request.LoggedAt, new Milliliters(request.AmountMilliliters));
        await _waterEntryRepository.AddAsync(entry, cancellationToken);
        return ToDto(entry);
    }

    /// <returns>The updated entry, or null when it does not exist or does not belong to <paramref name="userId"/>.</returns>
    public async Task<WaterEntryDto?> UpdateAsync(
        UserId userId,
        WaterEntryId id,
        UpdateWaterEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _waterEntryRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = WaterEntry.Create(id, userId, request.LoggedAt, new Milliliters(request.AmountMilliliters));
        await _waterEntryRepository.UpdateAsync(updated, cancellationToken);
        return ToDto(updated);
    }

    /// <returns>True when the entry existed and belonged to <paramref name="userId"/> and was deleted.</returns>
    public async Task<bool> DeleteAsync(UserId userId, WaterEntryId id, CancellationToken cancellationToken = default)
    {
        var existing = await _waterEntryRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await _waterEntryRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static WaterEntryDto ToDto(WaterEntry entry) => new(entry.Id.Value, entry.LoggedAt, entry.Amount.Value);
}
