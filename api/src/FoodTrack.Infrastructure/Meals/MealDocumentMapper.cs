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
        DateOnly.FromDateTime(document.Date),
        Enum.Parse<MealType>(document.MealType),
        items);

    public static MealDocument ToDocument(Meal meal) => new()
    {
        UserId = meal.UserId.Value,
        Date = DateTime.SpecifyKind(meal.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
        MealType = meal.MealType.ToString(),
    };
}
