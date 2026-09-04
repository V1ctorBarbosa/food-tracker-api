namespace FoodTrack.Application.WeightEntries;

public sealed record WeightEntryDto(string Id, DateTimeOffset LoggedAt, double WeightKilograms);
