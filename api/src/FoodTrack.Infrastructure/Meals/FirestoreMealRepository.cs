using FoodTrack.Application.Common;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Meals;

public sealed class FirestoreMealRepository : IMealRepository
{
    private const string CollectionName = "meals";
    private const string ItemsSubcollectionName = "items";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreMealRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<Meal?> GetByIdAsync(MealId id, UserId userId, CancellationToken cancellationToken = default)
    {
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(id.Value);
        var snapshot = await mealDocumentReference.GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<MealDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        var items = await LoadItemsAsync(mealDocumentReference, cancellationToken);
        return MealDocumentMapper.ToDomain(id, userId, document, items);
    }

    public async Task<IReadOnlyList<Meal>> ListByUserAsync(
        UserId userId,
        DayRange dayRange,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        var (fromInclusive, toExclusive) = LocalDayRange.ToInstants(dayRange, utcOffset);

        var query = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(MealDocument.UserId), userId.Value)
            .WhereGreaterThanOrEqualTo(nameof(MealDocument.LoggedAt), fromInclusive)
            .WhereLessThan(nameof(MealDocument.LoggedAt), toExclusive);

        var querySnapshot = await query.GetSnapshotAsync(cancellationToken);

        // Each meal's items live in its own subcollection, so loading them is inherently one
        // request per meal -- run them concurrently instead of one at a time to avoid paying for
        // that in latency too.
        var itemsByMeal = await Task.WhenAll(
            querySnapshot.Documents.Select(mealSnapshot => LoadItemsAsync(mealSnapshot.Reference, cancellationToken)));

        return querySnapshot.Documents
            .Zip(itemsByMeal, (mealSnapshot, items) => MealDocumentMapper.ToDomain(new MealId(mealSnapshot.Id), userId, mealSnapshot.ConvertTo<MealDocument>(), items))
            .OrderBy(meal => meal.LoggedAt)
            .ToList();
    }

    public async Task AddAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(meal.Id.Value);

        var batch = _firestoreDb.StartBatch();
        batch.Set(mealDocumentReference, MealDocumentMapper.ToDocument(meal));
        foreach (var item in meal.Items)
        {
            batch.Set(mealDocumentReference.Collection(ItemsSubcollectionName).Document(item.Id.Value), MealItemDocumentMapper.ToDocument(item));
        }

        await batch.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(meal.Id.Value);

        // A PUT is a full replace, so items removed from the meal must not linger in the
        // subcollection. Clearing the old set and writing the new one inside one transaction
        // (instead of sequential deletes/sets) means a failure partway through can never leave
        // the meal with its old items gone and the new ones missing.
        await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var existingItemsSnapshot = await transaction.GetSnapshotAsync(mealDocumentReference.Collection(ItemsSubcollectionName));

            transaction.Set(mealDocumentReference, MealDocumentMapper.ToDocument(meal));
            foreach (var itemDocument in existingItemsSnapshot.Documents)
            {
                transaction.Delete(itemDocument.Reference);
            }

            foreach (var item in meal.Items)
            {
                transaction.Set(mealDocumentReference.Collection(ItemsSubcollectionName).Document(item.Id.Value), MealItemDocumentMapper.ToDocument(item));
            }
        }, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(MealId id, CancellationToken cancellationToken = default)
    {
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(id.Value);

        await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var itemsSnapshot = await transaction.GetSnapshotAsync(mealDocumentReference.Collection(ItemsSubcollectionName));

            foreach (var itemDocument in itemsSnapshot.Documents)
            {
                transaction.Delete(itemDocument.Reference);
            }

            transaction.Delete(mealDocumentReference);
        }, cancellationToken: cancellationToken);
    }

    private static async Task<IReadOnlyList<MealItem>> LoadItemsAsync(
        DocumentReference mealDocumentReference,
        CancellationToken cancellationToken)
    {
        var itemsSnapshot = await mealDocumentReference.Collection(ItemsSubcollectionName).GetSnapshotAsync(cancellationToken);

        return itemsSnapshot.Documents
            .Select(doc => MealItemDocumentMapper.ToDomain(new MealItemId(doc.Id), doc.ConvertTo<MealItemDocument>()))
            .ToList();
    }
}
