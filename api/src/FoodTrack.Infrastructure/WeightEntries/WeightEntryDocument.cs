using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.WeightEntries;

[FirestoreData]
internal sealed class WeightEntryDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTimeOffset LoggedAt { get; set; }

    [FirestoreProperty]
    public double WeightKilograms { get; set; }
}
