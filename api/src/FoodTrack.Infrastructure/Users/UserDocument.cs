using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Users;

[FirestoreData]
internal sealed class UserDocument
{
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Age { get; set; }

    [FirestoreProperty]
    public int HeightCentimeters { get; set; }

    [FirestoreProperty]
    public string Sex { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ActivityLevel { get; set; } = string.Empty;

    [FirestoreProperty]
    public double? TargetWeightKilograms { get; set; }

    [FirestoreProperty]
    public int DesiredCalorieDeficit { get; set; }

    [FirestoreProperty]
    public int DailyCalorieTarget { get; set; }
}
