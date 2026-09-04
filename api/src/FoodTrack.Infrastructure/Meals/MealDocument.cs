using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Meals;

[FirestoreData]
internal sealed class MealDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    /// <summary>UTC midnight of the meal's date — Firestore has no dedicated date-only type.</summary>
    [FirestoreProperty]
    public DateTime Date { get; set; }

    [FirestoreProperty]
    public string MealType { get; set; } = string.Empty;
}
