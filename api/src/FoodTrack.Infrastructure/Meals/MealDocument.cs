using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Meals;

[FirestoreData]
internal sealed class MealDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTimeOffset LoggedAt { get; set; }

    [FirestoreProperty]
    public string MealType { get; set; } = string.Empty;
}
