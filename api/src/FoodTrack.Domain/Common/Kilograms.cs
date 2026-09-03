namespace FoodTrack.Domain.Common;

public readonly record struct Kilograms
{
    private const double MinValue = 1;
    private const double MaxValue = 500;

    public double Value { get; }

    public Kilograms(double value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Weight must be between {MinValue} and {MaxValue} kg.");
        }

        Value = value;
    }
}
