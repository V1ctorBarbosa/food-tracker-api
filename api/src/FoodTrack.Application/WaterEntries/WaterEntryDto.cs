namespace FoodTrack.Application.WaterEntries;

public sealed record WaterEntryDto(string Id, DateTimeOffset LoggedAt, int AmountMilliliters);
