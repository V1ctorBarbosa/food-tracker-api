namespace FoodTrack.Domain.Foods;

public readonly record struct FoodServingId
{
    public string Value { get; }

    public FoodServingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Food serving id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static FoodServingId NewId() => new(Guid.NewGuid().ToString());
}
