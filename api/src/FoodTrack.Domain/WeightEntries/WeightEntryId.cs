namespace FoodTrack.Domain.WeightEntries;

public readonly record struct WeightEntryId
{
    public string Value { get; }

    public WeightEntryId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Weight entry id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static WeightEntryId NewId() => new(Guid.NewGuid().ToString());
}
