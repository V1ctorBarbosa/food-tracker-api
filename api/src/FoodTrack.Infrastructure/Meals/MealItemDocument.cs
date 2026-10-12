using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Meals;

[FirestoreData]
internal sealed class MealItemDocument
{
    /// <summary>One of <see cref="MealItemOriginNames"/>. Null on items saved before estimated items existed, which are all catalog items.</summary>
    [FirestoreProperty]
    public string? Origin { get; set; }

    /// <summary>Set for catalog items only.</summary>
    [FirestoreProperty]
    public string? FoodId { get; set; }

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

    // The fields below are set for estimated items only: the food lives on the item itself.

    [FirestoreProperty]
    public string? Name { get; set; }

    [FirestoreProperty]
    public string? CaptureId { get; set; }

    [FirestoreProperty]
    public string? BaseUnit { get; set; }

    [FirestoreProperty]
    public int? CaloriesPerBaseUnit { get; set; }

    [FirestoreProperty]
    public double? ProteinPerBaseUnit { get; set; }

    [FirestoreProperty]
    public double? CarbsPerBaseUnit { get; set; }

    [FirestoreProperty]
    public double? FatPerBaseUnit { get; set; }
}
