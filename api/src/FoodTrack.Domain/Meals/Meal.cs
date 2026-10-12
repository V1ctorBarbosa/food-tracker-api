using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Domain.Meals;

/// <summary>
/// A meal that took place (docs section 10), aggregate root for its <see cref="MealItem"/>
/// children and the <see cref="MealCapture"/>s its estimated items came from. Totals are always
/// derived from the items here, so the aggregate can never disagree with itself. Persistence
/// keeps a copy of them on the meal document (see <see cref="ToSummary"/>) so lists can skip
/// loading the items; that copy is only ever written from this derivation, in the same operation
/// that writes the items.
/// </summary>
public sealed class Meal
{
    public const int MaximumItemCount = 50;

    /// <summary>Every capture is a paid AI estimate and possibly a stored photo, so one meal cannot pile them up.</summary>
    public const int MaximumCaptureCount = 5;

    public MealId Id { get; }

    public UserId UserId { get; }

    public DateTimeOffset LoggedAt { get; }

    public MealType MealType { get; }

    public IReadOnlyList<MealItem> Items { get; }

    /// <summary>
    /// The estimates this meal's AI items came from. A capture stays even after all of its items
    /// are removed, so its photo is still known and cleaned up when the meal is deleted.
    /// </summary>
    public IReadOnlyList<MealCapture> Captures { get; }

    public Kilocalories TotalCalories => new(Items.Sum(item => item.Calories.Value));

    public Grams TotalProtein => new(Items.Sum(item => item.Protein.Value));

    public Grams TotalCarbs => new(Items.Sum(item => item.Carbs.Value));

    public Grams TotalFat => new(Items.Sum(item => item.Fat.Value));

    private Meal(
        MealId id,
        UserId userId,
        DateTimeOffset loggedAt,
        MealType mealType,
        IReadOnlyList<MealItem> items,
        IReadOnlyList<MealCapture> captures)
    {
        Id = id;
        UserId = userId;
        LoggedAt = loggedAt;
        MealType = mealType;
        Items = items;
        Captures = captures;
    }

    public static Meal Create(MealId id, UserId userId, DateTimeOffset loggedAt, MealType mealType, IReadOnlyList<MealItem> items) =>
        Create(id, userId, loggedAt, mealType, items, []);

    /// <exception cref="ArgumentException">
    /// The item or capture count is outside its limit, a capture appears twice, or an estimated
    /// item refers to a capture this meal does not have.
    /// </exception>
    public static Meal Create(
        MealId id,
        UserId userId,
        DateTimeOffset loggedAt,
        MealType mealType,
        IReadOnlyList<MealItem> items,
        IReadOnlyList<MealCapture> captures)
    {
        EnsureItemCountIsAllowed(items.Count);
        EnsureCapturesAreAllowed(captures);
        EnsureEstimatedItemsBelongToCaptures(items, captures);

        return new Meal(id, userId, loggedAt, mealType, items, captures);
    }

    /// <summary>
    /// Checks the item count on its own so a caller can reject an oversized meal before paying to
    /// resolve each of its items. Every item costs database reads and a write, so the cap keeps
    /// one request from fanning out into thousands of them.
    /// </summary>
    /// <exception cref="ArgumentException">The count is zero or above <see cref="MaximumItemCount"/>.</exception>
    public static void EnsureItemCountIsAllowed(int itemCount)
    {
        if (itemCount == 0)
        {
            throw new ArgumentException("A meal must have at least one item.", nameof(itemCount));
        }

        if (itemCount > MaximumItemCount)
        {
            throw new ArgumentException($"A meal cannot have more than {MaximumItemCount} items.", nameof(itemCount));
        }
    }

    public MealSummary ToSummary() => new(Id, LoggedAt, MealType, TotalCalories, TotalProtein, TotalCarbs, TotalFat);

    private static void EnsureCapturesAreAllowed(IReadOnlyList<MealCapture> captures)
    {
        if (captures.Count > MaximumCaptureCount)
        {
            throw new ArgumentException($"A meal cannot have more than {MaximumCaptureCount} captures.", nameof(captures));
        }

        var distinctCaptureCount = captures.Select(capture => capture.Id).Distinct().Count();
        if (distinctCaptureCount != captures.Count)
        {
            throw new ArgumentException("A meal cannot have the same capture twice.", nameof(captures));
        }
    }

    private static void EnsureEstimatedItemsBelongToCaptures(IReadOnlyList<MealItem> items, IReadOnlyList<MealCapture> captures)
    {
        var captureIds = captures.Select(capture => capture.Id).ToHashSet();
        var hasItemFromUnknownCapture = items
            .Select(item => item.Origin)
            .OfType<EstimatedFoodOrigin>()
            .Any(origin => !captureIds.Contains(origin.CaptureId));

        if (hasItemFromUnknownCapture)
        {
            throw new ArgumentException("Every estimated item must come from one of the meal's captures.", nameof(items));
        }
    }
}
