using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Foods;

[FirestoreData]
internal sealed class FoodServingDocument
{
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public double Quantity { get; set; }

    [FirestoreProperty]
    public string Unit { get; set; } = string.Empty;
}
