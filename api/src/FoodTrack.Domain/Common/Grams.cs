namespace FoodTrack.Domain.Common;

/// <summary>A quantity of a nutrient (protein, carbs, or fat) — distinct from body weight (<see cref="Kilograms"/>).</summary>
public readonly record struct Grams
{
    private const double MinValue = 0;
    private const double MaxValue = 1_000;

    public double Value { get; }

    public Grams(double value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Amount must be between {MinValue} and {MaxValue} g.");
        }

        Value = value;
    }
}
