namespace FoodTrack.Application.WaterEntries;

public sealed record UpdateWaterEntryRequest(DateTimeOffset LoggedAt, int AmountMilliliters);
