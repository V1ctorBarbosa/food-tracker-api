using FoodTrack.Application.Common;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.WaterEntries;

public sealed class FirestoreWaterEntryRepository : IWaterEntryRepository
{
    private const string CollectionName = "waterEntries";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreWaterEntryRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<WaterEntry?> GetByIdAsync(WaterEntryId id, UserId userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName).Document(id.Value).GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<WaterEntryDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        return WaterEntryDocumentMapper.ToDomain(id, document);
    }

    public async Task<IReadOnlyList<WaterEntry>> ListByUserAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        var (fromInclusive, toExclusive) = LocalDayRange.ToInstants(from, to, utcOffset);

        var query = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(WaterEntryDocument.UserId), userId.Value)
            .WhereGreaterThanOrEqualTo(nameof(WaterEntryDocument.LoggedAt), fromInclusive)
            .WhereLessThan(nameof(WaterEntryDocument.LoggedAt), toExclusive);

        var snapshot = await query.GetSnapshotAsync(cancellationToken);

        return snapshot.Documents
            .Select(doc => WaterEntryDocumentMapper.ToDomain(new WaterEntryId(doc.Id), doc.ConvertTo<WaterEntryDocument>()))
            .OrderBy(entry => entry.LoggedAt)
            .ToList();
    }

    public async Task AddAsync(WaterEntry entry, CancellationToken cancellationToken = default)
    {
        var document = WaterEntryDocumentMapper.ToDocument(entry);
        await _firestoreDb.Collection(CollectionName)
            .Document(entry.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(WaterEntry entry, CancellationToken cancellationToken = default)
    {
        var document = WaterEntryDocumentMapper.ToDocument(entry);
        await _firestoreDb.Collection(CollectionName)
            .Document(entry.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(WaterEntryId id, CancellationToken cancellationToken = default)
    {
        await _firestoreDb.Collection(CollectionName)
            .Document(id.Value)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
