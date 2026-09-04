namespace FoodTrack.Domain.Foods;

public readonly record struct FoodId
{
    public string Value { get; }

    public FoodId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Food id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static FoodId NewId() => new(Guid.NewGuid().ToString());
}
