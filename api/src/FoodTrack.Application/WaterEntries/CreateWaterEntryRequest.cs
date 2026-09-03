namespace FoodTrack.Application.WaterEntries;

public sealed record CreateWaterEntryRequest(DateTimeOffset LoggedAt, int AmountMilliliters);
