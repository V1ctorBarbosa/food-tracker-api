namespace FoodTrack.Application.WeightEntries;

public sealed record CreateWeightEntryRequest(double WeightKilograms, DateTimeOffset? LoggedAt = null);
