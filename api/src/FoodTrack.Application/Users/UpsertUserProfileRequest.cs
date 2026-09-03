namespace FoodTrack.Application.Users;

public sealed record UpsertUserProfileRequest(
    string Name,
    int Age,
    int HeightCentimeters,
    string Sex,
    string ActivityLevel,
    double? TargetWeightKilograms,
    int DesiredCalorieDeficit,
    int DailyCalorieTarget);
