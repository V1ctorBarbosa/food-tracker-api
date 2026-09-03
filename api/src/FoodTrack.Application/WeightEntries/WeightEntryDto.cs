namespace FoodTrack.Application.WeightEntries;

public sealed record WeightEntryDto(string Id, DateOnly Date, double WeightKilograms);
