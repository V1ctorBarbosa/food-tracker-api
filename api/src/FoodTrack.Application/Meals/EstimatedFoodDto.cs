namespace FoodTrack.Application.Meals;

/// <summary>Same shape as <see cref="EstimatedFoodRequest"/>, so a client can send an item back unchanged.</summary>
public sealed record EstimatedFoodDto(
    string Name,
    string CaptureId,
    string BaseUnit,
    int CaloriesPerBaseUnit,
    double ProteinPerBaseUnit,
    double CarbsPerBaseUnit,
    double FatPerBaseUnit);
