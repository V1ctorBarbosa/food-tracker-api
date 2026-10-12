using FoodTrack.Application.MealEstimates;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure.MealEstimates;

/// <summary>
/// Keeps the month's count in Firestore, not in memory, because the API can run as several
/// function instances at once. The read and the increment share one transaction, so two estimates
/// arriving together can never both take the last slot.
/// </summary>
public sealed class FirestoreMealEstimateQuota : IMealEstimateQuota
{
    private const string CollectionName = "mealEstimateUsage";

    private readonly FirestoreDb _firestoreDb;
    private readonly int _monthlyLimit;

    public FirestoreMealEstimateQuota(FirestoreDb firestoreDb, IOptions<MealEstimateAccessOptions> options)
    {
        _firestoreDb = firestoreDb;
        _monthlyLimit = options.Value.MonthlyEstimateLimit;
    }

    public Task<bool> TryConsumeAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var usageReference = _firestoreDb.Collection(CollectionName).Document(MealEstimateUsageMonth.KeyFor(now));

        return _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(usageReference, cancellationToken);
            var usedCount = ReadCount(snapshot);
            if (!MealEstimateUsageMonth.HasRoomFor(usedCount, _monthlyLimit))
            {
                return false;
            }

            transaction.Set(usageReference, new MealEstimateUsageDocument { Count = usedCount + 1 });
            return true;
        }, cancellationToken: cancellationToken);
    }

    private static int ReadCount(DocumentSnapshot snapshot)
    {
        if (!snapshot.Exists)
        {
            return 0;
        }

        return snapshot.ConvertTo<MealEstimateUsageDocument>().Count;
    }
}
