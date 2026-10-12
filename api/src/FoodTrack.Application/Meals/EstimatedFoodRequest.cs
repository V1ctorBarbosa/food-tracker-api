namespace FoodTrack.Application.Meals;

/// <summary>
/// An AI-estimated food sent back to be logged: the values the estimate returned, per 100 g or
/// 100 ml of <see cref="BaseUnit"/>, from the capture <see cref="CaptureId"/>.
/// </summary>
public sealed record EstimatedFoodRequest(
    string Name,
    string CaptureId,
    string BaseUnit,
    int CaloriesPerBaseUnit,
    double ProteinPerBaseUnit,
    double CarbsPerBaseUnit,
    double FatPerBaseUnit);
