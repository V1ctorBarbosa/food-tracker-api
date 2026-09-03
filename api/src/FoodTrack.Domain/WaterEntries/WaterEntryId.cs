namespace FoodTrack.Domain.WaterEntries;

public readonly record struct WaterEntryId
{
    public string Value { get; }

    public WaterEntryId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Water entry id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static WaterEntryId NewId() => new(Guid.NewGuid().ToString());
}
