using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A meal that took place (docs section 10), aggregate root for its <see cref="MealItem"/>
/// children. Totals are always derived from the items here, so the aggregate can never disagree
/// with itself. Persistence keeps a copy of them on the meal document (see
/// <see cref="ToSummary"/>) so lists can skip loading the items; that copy is only ever written
/// from this derivation, in the same operation that writes the items.
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

    public MealSummary ToSummary() => new(Id, LoggedAt, MealType, TotalCalories, TotalProtein, TotalCarbs, TotalFat);
}
