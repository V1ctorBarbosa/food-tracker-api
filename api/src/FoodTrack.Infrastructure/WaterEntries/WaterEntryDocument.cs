using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.WaterEntries;

[FirestoreData]
internal sealed class WaterEntryDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTimeOffset LoggedAt { get; set; }

    [FirestoreProperty]
    public int AmountMilliliters { get; set; }
}
