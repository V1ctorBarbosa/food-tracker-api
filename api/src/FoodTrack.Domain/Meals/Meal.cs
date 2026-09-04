using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A meal that took place (docs section 10), aggregate root for its <see cref="MealItem"/>
/// children. Totals are derived from the items rather than stored -- the docs note that
/// persisting them is possible "as long as the API keeps them consistent when items change";
/// deriving them avoids that whole class of bug for the size this project needs.
/// </summary>
public sealed class Meal
{
    public MealId Id { get; }

    public UserId UserId { get; }

    public DateTimeOffset LoggedAt { get; }

    public MealType MealType { get; }

    public IReadOnlyList<MealItem> Items { get; }

    public Kilocalories TotalCalories => new(Items.Sum(item => item.Calories.Value));

    public Grams TotalProtein => new(Items.Sum(item => item.Protein.Value));

    public Grams TotalCarbs => new(Items.Sum(item => item.Carbs.Value));

    public Grams TotalFat => new(Items.Sum(item => item.Fat.Value));

    private Meal(MealId id, UserId userId, DateTimeOffset loggedAt, MealType mealType, IReadOnlyList<MealItem> items)
    {
        Id = id;
        UserId = userId;
        LoggedAt = loggedAt;
        MealType = mealType;
        Items = items;
    }

    public static Meal Create(MealId id, UserId userId, DateTimeOffset loggedAt, MealType mealType, IReadOnlyList<MealItem> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("A meal must have at least one item.", nameof(items));
        }

        return new Meal(id, userId, loggedAt, mealType, items);
    }
}
