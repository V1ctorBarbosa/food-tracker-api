using FoodTrack.Application.Foods;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;
using FoodTrack.Infrastructure.Firebase;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Foods;

public sealed class FirestoreFoodRepository : IFoodRepository
{
    private const string CollectionName = "foods";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreFoodRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken = default)
    {
        var foodDocumentReference = _firestoreDb.Collection(CollectionName).Document(id.Value);
        var snapshot = await foodDocumentReference.GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists)
        {
            return null;
        }

        return FoodDocumentMapper.ToDomain(id, snapshot.ConvertTo<FoodDocument>());
    }

    public async Task<IReadOnlyList<Food>> SearchByNameAsync(string query, CancellationToken cancellationToken = default)
    {
        // Firestore has no native contains/text-search operator, only the standard "starts with"
        // trick (U+F8FF sorts above every normal indexed character, so the range
        // [firstWord, firstWord + U+F8FF] captures every NameLower starting with it). A query can
        // have several words ("arroz cozido"), so the range narrows candidates by the first word
        // only -- cheap and index-backed -- and FoodNameMatcher then confirms every other word is
        // present too, in any order/position, over that (small) candidate set.
        var queryLower = query.Trim().ToLowerInvariant();
        var firstWord = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? queryLower;
        var prefixUpperBound = firstWord + '';

        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereGreaterThanOrEqualTo(nameof(FoodDocument.NameLower), firstWord)
            .WhereLessThanOrEqualTo(nameof(FoodDocument.NameLower), prefixUpperBound)
            .GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(foodSnapshot => (Snapshot: foodSnapshot, Document: foodSnapshot.ConvertTo<FoodDocument>()))
            .Where(match => FoodNameMatcher.MatchesAllWords(match.Document.NameLower, queryLower))
            .Select(match => FoodDocumentMapper.ToDomain(new FoodId(match.Snapshot.Id), match.Document))
            .ToList();
    }

    public async Task<Food?> GetBySourceAndExternalIdAsync(FoodSource source, string externalId, CancellationToken cancellationToken = default)
    {
        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(FoodDocument.Source), source.ToString())
            .WhereEqualTo(nameof(FoodDocument.ExternalId), externalId)
            .Limit(1)
            .GetSnapshotAsync(cancellationToken);

        var documentSnapshot = querySnapshot.Documents.FirstOrDefault();
        if (documentSnapshot is null)
        {
            return null;
        }

        return FoodDocumentMapper.ToDomain(new FoodId(documentSnapshot.Id), documentSnapshot.ConvertTo<FoodDocument>());
    }

    public async Task SaveAsync(Food food, CancellationToken cancellationToken = default)
    {
        var foodDocumentReference = _firestoreDb.Collection(CollectionName).Document(food.Id.Value);
        await foodDocumentReference.SetAsync(FoodDocumentMapper.ToDocument(food), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Food>> ListCustomFoodsOwnedByAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(FoodDocument.OwnerId), userId.Value)
            .GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(foodSnapshot => FoodDocumentMapper.ToDomain(new FoodId(foodSnapshot.Id), foodSnapshot.ConvertTo<FoodDocument>()))
            .ToList();
    }

    public async Task<IReadOnlyList<Food>> ListBySourceAsync(FoodSource source, CancellationToken cancellationToken = default)
    {
        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(FoodDocument.Source), source.ToString())
            .GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(foodSnapshot => FoodDocumentMapper.ToDomain(new FoodId(foodSnapshot.Id), foodSnapshot.ConvertTo<FoodDocument>()))
            .ToList();
    }

    public async Task DeleteCustomFoodsOwnedByAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var query = _firestoreDb.Collection(CollectionName).WhereEqualTo(nameof(FoodDocument.OwnerId), userId.Value);
        await FirestoreBulkDeletion.DeleteMatchingAsync(_firestoreDb, query, cancellationToken);
    }
}
