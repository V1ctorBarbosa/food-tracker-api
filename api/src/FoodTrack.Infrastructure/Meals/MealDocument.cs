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

    // Copies of the totals derived from the items, written together with them. Nullable because
    // meals saved before these existed have none, and a missing total must not read as zero.
    [FirestoreProperty]
    public int? TotalCalories { get; set; }

    [FirestoreProperty]
    public double? TotalProtein { get; set; }

    [FirestoreProperty]
    public double? TotalCarbs { get; set; }

    [FirestoreProperty]
    public double? TotalFat { get; set; }
}
