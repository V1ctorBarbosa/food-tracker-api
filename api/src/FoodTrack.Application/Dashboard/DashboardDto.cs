namespace FoodTrack.Application.Dashboard;

public sealed record DashboardDto(
    int DailyCalorieTarget,
    double? CurrentWeightKilograms,
    double? TargetWeightKilograms,
    int? DailyWaterTargetMilliliters,
    double? DailyProteinTargetGrams,
    double? DailyCarbsTargetGrams,
    double? DailyFatTargetGrams,
    IReadOnlyList<WeightHistoryEntryDto> WeightHistory,
    IReadOnlyList<NutritionHistoryEntryDto> NutritionHistory);
