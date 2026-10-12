namespace FoodTrack.Domain.Meals;

/// <summary>What the user wrote about a meal for the AI estimate: preparation, hidden ingredients, known amounts.</summary>
public readonly record struct MealDescription
{
    /// <summary>Bounds the text sent to the paid model on every estimate and re-run.</summary>
    public const int MaximumLength = 500;

    public string Value { get; }

    public MealDescription(string value)
    {
        var trimmedValue = value?.Trim() ?? string.Empty;
        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException("Meal description cannot be empty.", nameof(value));
        }

        if (trimmedValue.Length > MaximumLength)
        {
            throw new ArgumentException($"Meal description cannot be longer than {MaximumLength} characters.", nameof(value));
        }

        Value = trimmedValue;
    }
}
