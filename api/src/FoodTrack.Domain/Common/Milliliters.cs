namespace FoodTrack.Domain.Common;

public readonly record struct Milliliters
{
    private const int MinValue = 1;
    private const int MaxValue = 5_000;

    public int Value { get; }

    public Milliliters(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Amount must be between {MinValue} and {MaxValue} ml.");
        }

        Value = value;
    }
}
