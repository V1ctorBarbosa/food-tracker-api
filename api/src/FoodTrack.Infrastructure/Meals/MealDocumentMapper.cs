using FoodTrack.Domain.Common;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Infrastructure.Meals;

/// <summary>
/// Pure mapping between the domain's <see cref="Meal"/> aggregate (including its items) and
/// its Firestore persistence representation. Kept free of any Firestore I/O so it can be
/// unit tested without a live database.
/// </summary>
internal static class MealDocumentMapper
{
    public static Meal ToDomain(MealId id, UserId userId, MealDocument document, IReadOnlyList<MealItem> items) => Meal.Create(
        id,
        userId,
        document.LoggedAt,
        Enum.Parse<MealType>(document.MealType),
        items);

    public static MealDocument ToDocument(Meal meal) => new()
    {
        UserId = meal.UserId.Value,
        LoggedAt = meal.LoggedAt,
        MealType = meal.MealType.ToString(),
        TotalCalories = meal.TotalCalories.Value,
        TotalProtein = meal.TotalProtein.Value,
        TotalCarbs = meal.TotalCarbs.Value,
        TotalFat = meal.TotalFat.Value,
    };

    /// <summary>
    /// The summary read straight from the document's stored totals, or null when the document
    /// predates them (any total missing) and the items must be loaded to derive them.
    /// </summary>
    public static MealSummary? TryToSummary(MealId id, MealDocument document)
    {
        if (document.TotalCalories is not { } calories
            || document.TotalProtein is not { } protein
            || document.TotalCarbs is not { } carbs
            || document.TotalFat is not { } fat)
        {
            return null;
        }

        return new MealSummary(
            id,
            document.LoggedAt,
            Enum.Parse<MealType>(document.MealType),
            new Kilocalories(calories),
            new Grams(protein),
            new Grams(carbs),
            new Grams(fat));
    }
}
