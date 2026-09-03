namespace FoodTrack.Application.Users;

public sealed record UserProfileDto(
    string Id,
    string Name,
    int Age,
    int HeightCentimeters,
    string Sex,
    string ActivityLevel,
    double? TargetWeightKilograms,
    int DesiredCalorieDeficit,
    int DailyCalorieTarget);
