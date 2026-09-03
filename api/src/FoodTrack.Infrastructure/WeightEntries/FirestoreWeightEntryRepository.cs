using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.WeightEntries;

public sealed class FirestoreWeightEntryRepository : IWeightEntryRepository
{
    private const string CollectionName = "weightEntries";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreWeightEntryRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<WeightEntry?> GetByIdAsync(WeightEntryId id, UserId userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName).Document(id.Value).GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<WeightEntryDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        return WeightEntryDocumentMapper.ToDomain(id, document);
    }

    public async Task<IReadOnlyList<WeightEntry>> ListByUserAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var query = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(WeightEntryDocument.UserId), userId.Value)
            .WhereGreaterThanOrEqualTo(nameof(WeightEntryDocument.Date), fromUtc)
            .WhereLessThanOrEqualTo(nameof(WeightEntryDocument.Date), toUtc);

        var snapshot = await query.GetSnapshotAsync(cancellationToken);

        return snapshot.Documents
            .Select(doc => WeightEntryDocumentMapper.ToDomain(new WeightEntryId(doc.Id), doc.ConvertTo<WeightEntryDocument>()))
            .OrderBy(entry => entry.Date)
            .ToList();
    }

    public async Task AddAsync(WeightEntry entry, CancellationToken cancellationToken = default)
    {
        var document = WeightEntryDocumentMapper.ToDocument(entry);
        await _firestoreDb.Collection(CollectionName)
            .Document(entry.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default)
    {
        var document = WeightEntryDocumentMapper.ToDocument(entry);
        await _firestoreDb.Collection(CollectionName)
            .Document(entry.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(WeightEntryId id, CancellationToken cancellationToken = default)
    {
        await _firestoreDb.Collection(CollectionName)
            .Document(id.Value)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
