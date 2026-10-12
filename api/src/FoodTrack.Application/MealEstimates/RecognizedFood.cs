namespace FoodTrack.Application.MealEstimates;

/// <summary>One food as the estimator described it: an amount, and nutrition per 100 g or 100 ml of <see cref="BaseUnit"/>.</summary>
public sealed record RecognizedFood(
    string Name,
    double Quantity,
    string BaseUnit,
    double CaloriesPerBaseUnit,
    double ProteinPerBaseUnit,
    double CarbsPerBaseUnit,
    double FatPerBaseUnit);
