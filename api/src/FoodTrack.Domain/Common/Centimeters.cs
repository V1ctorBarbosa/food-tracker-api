namespace FoodTrack.Domain.Common;

public readonly record struct Centimeters
{
    private const int MinValue = 50;
    private const int MaxValue = 250;

    public int Value { get; }

    public Centimeters(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Height must be between {MinValue} and {MaxValue} cm.");
        }

        Value = value;
    }
}
