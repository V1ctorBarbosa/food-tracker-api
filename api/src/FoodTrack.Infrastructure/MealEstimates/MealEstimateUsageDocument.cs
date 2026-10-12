using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.MealEstimates;

/// <summary>How many estimates ran in one calendar month; the document id is the month (see <see cref="MealEstimateUsageMonth"/>).</summary>
[FirestoreData]
internal sealed class MealEstimateUsageDocument
{
    [FirestoreProperty]
    public int Count { get; set; }
}
