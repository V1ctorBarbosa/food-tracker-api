using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Infrastructure.WaterEntries;

/// <summary>
/// Pure mapping between the domain's <see cref="WaterEntry"/> and its Firestore
/// persistence representation. Kept free of any Firestore I/O so it can be unit tested
/// without a live database.
/// </summary>
internal static class WaterEntryDocumentMapper
{
    public static WaterEntry ToDomain(WaterEntryId id, WaterEntryDocument document) => WaterEntry.Create(
        id,
        new UserId(document.UserId),
        document.LoggedAt,
        new Milliliters(document.AmountMilliliters));

    public static WaterEntryDocument ToDocument(WaterEntry entry) => new()
    {
        UserId = entry.UserId.Value,
        LoggedAt = entry.LoggedAt,
        AmountMilliliters = entry.Amount.Value,
    };
}
