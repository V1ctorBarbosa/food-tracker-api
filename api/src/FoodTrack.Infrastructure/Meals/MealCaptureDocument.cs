using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Meals;

[FirestoreData]
internal sealed class MealCaptureDocument
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool HasPhoto { get; set; }

    [FirestoreProperty]
    public string? Description { get; set; }
}
