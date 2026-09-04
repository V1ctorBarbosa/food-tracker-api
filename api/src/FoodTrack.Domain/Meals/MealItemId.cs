namespace FoodTrack.Domain.Meals;

public readonly record struct MealItemId
{
    public string Value { get; }

    public MealItemId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Meal item id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static MealItemId NewId() => new(Guid.NewGuid().ToString());
}
