using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Infrastructure.Meals;

internal static class MealItemDocumentMapper
{
    public static MealItem ToDomain(MealItemId id, MealItemDocument document) => MealItem.Create(
        id,
        new FoodId(document.FoodId),
        document.Quantity,
        Enum.Parse<MeasurementUnit>(document.Unit),
        new Kilocalories(document.Calories),
        new Grams(document.Protein),
        new Grams(document.Carbs),
        new Grams(document.Fat));

    /// <summary>The stored unit name of an item, moved to <paramref name="category"/> with <see cref="MeasurementUnitReclassification"/>.</summary>
    public static string ReclassifyUnit(string storedUnit, MeasurementUnitCategory category) =>
        MeasurementUnitReclassification.Reclassify(Enum.Parse<MeasurementUnit>(storedUnit), category).ToString();

    public static MealItemDocument ToDocument(MealItem item) => new()
    {
        FoodId = item.FoodId.Value,
        Quantity = item.Quantity,
        Unit = item.Unit.ToString(),
        Calories = item.Calories.Value,
        Protein = item.Protein.Value,
        Carbs = item.Carbs.Value,
        Fat = item.Fat.Value,
    };
}
