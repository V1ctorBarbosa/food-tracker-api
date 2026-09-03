namespace FoodTrack.Domain.Common;

public readonly record struct Kilocalories
{
    private const int MinValue = 0;
    private const int MaxValue = 10_000;

    public int Value { get; }

    public Kilocalories(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Calorie amount must be between {MinValue} and {MaxValue} kcal.");
        }

        Value = value;
    }
}
