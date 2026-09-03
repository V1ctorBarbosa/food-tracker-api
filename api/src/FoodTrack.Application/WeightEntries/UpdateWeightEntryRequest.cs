namespace FoodTrack.Application.WeightEntries;

public sealed record UpdateWeightEntryRequest(DateOnly Date, double WeightKilograms);
