using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;

namespace FoodTrack.Infrastructure.WeightEntries;

/// <summary>
/// Pure mapping between the domain's <see cref="WeightEntry"/> and its Firestore
/// persistence representation. Kept free of any Firestore I/O so it can be unit tested
/// without a live database.
/// </summary>
internal static class WeightEntryDocumentMapper
{
    public static WeightEntry ToDomain(WeightEntryId id, WeightEntryDocument document) => WeightEntry.Create(
        id,
        new UserId(document.UserId),
        document.LoggedAt,
        new Kilograms(document.WeightKilograms));

    public static WeightEntryDocument ToDocument(WeightEntry entry) => new()
    {
        UserId = entry.UserId.Value,
        LoggedAt = entry.LoggedAt,
        WeightKilograms = entry.Weight.Value,
    };
}
