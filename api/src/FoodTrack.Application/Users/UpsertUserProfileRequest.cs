namespace FoodTrack.Application.Users;

public sealed record UpsertUserProfileRequest(
    string Name,
    int Age,
    int HeightCentimeters,
    string Gender,
    string ActivityLevel,
    double? TargetWeightKilograms,
    int DesiredCalorieDeficit,
    int DailyCalorieTarget,
    int? DailyWaterTargetMilliliters,
    double? DailyProteinTargetGrams,
    double? DailyCarbsTargetGrams,
    double? DailyFatTargetGrams);
