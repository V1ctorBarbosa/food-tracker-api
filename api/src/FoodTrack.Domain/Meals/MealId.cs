namespace FoodTrack.Domain.Meals;

public readonly record struct MealId
{
    public string Value { get; }

    public MealId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Meal id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static MealId NewId() => new(Guid.NewGuid().ToString());
}
