using FoodTrack.Application.Foods;
using FoodTrack.Domain.Foods;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Foods;

public sealed class FirestoreFoodRepository : IFoodRepository
{
    private const string CollectionName = "foods";
    private const string ServingsSubcollectionName = "servings";

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

        var servings = await LoadServingsAsync(foodDocumentReference, cancellationToken);
        return FoodDocumentMapper.ToDomain(id, snapshot.ConvertTo<FoodDocument>(), servings);
    }

    public async Task<IReadOnlyList<Food>> SearchByNameAsync(string query, CancellationToken cancellationToken = default)
    {
        // Firestore has no native prefix/text-search operator, so this uses the standard
        // "starts with" trick: U+F8FF sorts above every normal indexed character, so the
        // range [queryLower, queryLower + U+F8FF] captures every NameLower starting with it.
        var queryLower = query.Trim().ToLowerInvariant();
        var prefixUpperBound = queryLower + '';

        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereGreaterThanOrEqualTo(nameof(FoodDocument.NameLower), queryLower)
            .WhereLessThanOrEqualTo(nameof(FoodDocument.NameLower), prefixUpperBound)
            .GetSnapshotAsync(cancellationToken);

        var foods = new List<Food>();
        foreach (var foodSnapshot in querySnapshot.Documents)
        {
            var foodId = new FoodId(foodSnapshot.Id);
            var servings = await LoadServingsAsync(foodSnapshot.Reference, cancellationToken);
            foods.Add(FoodDocumentMapper.ToDomain(foodId, foodSnapshot.ConvertTo<FoodDocument>(), servings));
        }

        return foods;
    }

    public async Task SaveAsync(Food food, CancellationToken cancellationToken = default)
    {
        var foodDocumentReference = _firestoreDb.Collection(CollectionName).Document(food.Id.Value);
        await foodDocumentReference.SetAsync(FoodDocumentMapper.ToDocument(food), cancellationToken: cancellationToken);

        foreach (var serving in food.Servings)
        {
            await foodDocumentReference.Collection(ServingsSubcollectionName)
                .Document(serving.Id.Value)
                .SetAsync(FoodServingDocumentMapper.ToDocument(serving), cancellationToken: cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<FoodServing>> LoadServingsAsync(
        DocumentReference foodDocumentReference,
        CancellationToken cancellationToken)
    {
        var servingsSnapshot = await foodDocumentReference.Collection(ServingsSubcollectionName).GetSnapshotAsync(cancellationToken);

        return servingsSnapshot.Documents
            .Select(doc => FoodServingDocumentMapper.ToDomain(new FoodServingId(doc.Id), doc.ConvertTo<FoodServingDocument>()))
            .ToList();
    }
}
