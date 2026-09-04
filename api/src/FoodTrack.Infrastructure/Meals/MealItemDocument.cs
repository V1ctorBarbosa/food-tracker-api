using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Meals;

[FirestoreData]
internal sealed class MealItemDocument
{
    [FirestoreProperty]
    public string FoodId { get; set; } = string.Empty;

    [FirestoreProperty]
    public double Quantity { get; set; }

    [FirestoreProperty]
    public string Unit { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Calories { get; set; }

    [FirestoreProperty]
    public double Protein { get; set; }

    [FirestoreProperty]
    public double Carbs { get; set; }

    [FirestoreProperty]
    public double Fat { get; set; }
}
