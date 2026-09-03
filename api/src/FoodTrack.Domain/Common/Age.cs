namespace FoodTrack.Domain.Common;

public readonly record struct Age
{
    private const int MinValue = 1;
    private const int MaxValue = 120;

    public int Value { get; }

    public Age(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Age must be between {MinValue} and {MaxValue}.");
        }

        Value = value;
    }
}
