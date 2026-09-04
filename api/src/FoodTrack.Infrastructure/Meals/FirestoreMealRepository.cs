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
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = new DateTimeOffset(DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        var toExclusiveUtc = new DateTimeOffset(DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));

        var query = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(MealDocument.UserId), userId.Value)
            .WhereGreaterThanOrEqualTo(nameof(MealDocument.LoggedAt), fromUtc)
            .WhereLessThan(nameof(MealDocument.LoggedAt), toExclusiveUtc);

        var querySnapshot = await query.GetSnapshotAsync(cancellationToken);

        var meals = new List<Meal>();
        foreach (var mealSnapshot in querySnapshot.Documents)
        {
            var mealId = new MealId(mealSnapshot.Id);
            var items = await LoadItemsAsync(mealSnapshot.Reference, cancellationToken);
            meals.Add(MealDocumentMapper.ToDomain(mealId, userId, mealSnapshot.ConvertTo<MealDocument>(), items));
        }

        return meals.OrderBy(meal => meal.LoggedAt).ToList();
    }

    public async Task AddAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        await SaveMealAndItemsAsync(meal, cancellationToken);
    }

    public async Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        // A PUT is a full replace, so items removed from the meal must not linger in the
        // subcollection -- clear it before writing the current set back.
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(meal.Id.Value);
        var existingItemsSnapshot = await mealDocumentReference.Collection(ItemsSubcollectionName).GetSnapshotAsync(cancellationToken);
        foreach (var itemDocument in existingItemsSnapshot.Documents)
        {
            await itemDocument.Reference.DeleteAsync(cancellationToken: cancellationToken);
        }

        await SaveMealAndItemsAsync(meal, cancellationToken);
    }

    public async Task DeleteAsync(MealId id, CancellationToken cancellationToken = default)
    {
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(id.Value);
        var itemsSnapshot = await mealDocumentReference.Collection(ItemsSubcollectionName).GetSnapshotAsync(cancellationToken);
        foreach (var itemDocument in itemsSnapshot.Documents)
        {
            await itemDocument.Reference.DeleteAsync(cancellationToken: cancellationToken);
        }

        await mealDocumentReference.DeleteAsync(cancellationToken: cancellationToken);
    }

    private async Task SaveMealAndItemsAsync(Meal meal, CancellationToken cancellationToken)
    {
        var mealDocumentReference = _firestoreDb.Collection(CollectionName).Document(meal.Id.Value);
        await mealDocumentReference.SetAsync(MealDocumentMapper.ToDocument(meal), cancellationToken: cancellationToken);

        foreach (var item in meal.Items)
        {
            await mealDocumentReference.Collection(ItemsSubcollectionName)
                .Document(item.Id.Value)
                .SetAsync(MealItemDocumentMapper.ToDocument(item), cancellationToken: cancellationToken);
        }
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
