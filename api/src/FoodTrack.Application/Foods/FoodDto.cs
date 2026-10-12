namespace FoodTrack.Application.Foods;

public sealed record FoodDto(
    string Id,
    string Name,
    string Source,
    double BaseUnitQuantity,
    string BaseUnitType,
    int CaloriesPerBaseUnit,
    double ProteinPerBaseUnit,
    double CarbsPerBaseUnit,
    double FatPerBaseUnit);
