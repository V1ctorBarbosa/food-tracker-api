using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.WeightEntries;

[FirestoreData]
internal sealed class WeightEntryDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    /// <summary>UTC midnight of the entry's date — Firestore has no dedicated date-only type.</summary>
    [FirestoreProperty]
    public DateTime Date { get; set; }

    [FirestoreProperty]
    public double WeightKilograms { get; set; }
}
