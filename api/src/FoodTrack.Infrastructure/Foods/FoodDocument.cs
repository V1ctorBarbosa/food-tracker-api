using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Foods;

[FirestoreData]
internal sealed class FoodDocument
{
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    /// <summary>Lowercased name, queried with a range for a Firestore-native "starts with" search.</summary>
    [FirestoreProperty]
    public string NameLower { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Source { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? ExternalId { get; set; }

    /// <summary>Owner of a personal (Custom) food; absent/null for the shared cache.</summary>
    [FirestoreProperty]
    public string? OwnerId { get; set; }

    [FirestoreProperty]
    public double BaseUnitQuantity { get; set; }

    [FirestoreProperty]
    public string BaseUnitType { get; set; } = string.Empty;

    [FirestoreProperty]
    public int CaloriesPerBaseUnit { get; set; }

    [FirestoreProperty]
    public double ProteinPerBaseUnit { get; set; }

    [FirestoreProperty]
    public double CarbsPerBaseUnit { get; set; }

    [FirestoreProperty]
    public double FatPerBaseUnit { get; set; }
}
