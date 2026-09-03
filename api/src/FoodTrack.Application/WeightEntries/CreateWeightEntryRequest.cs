namespace FoodTrack.Application.WeightEntries;

public sealed record CreateWeightEntryRequest(DateOnly Date, double WeightKilograms);
