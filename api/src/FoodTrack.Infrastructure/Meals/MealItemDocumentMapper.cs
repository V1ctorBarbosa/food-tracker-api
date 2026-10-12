using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Infrastructure.Meals;

internal static class MealItemDocumentMapper
{
    public static MealItem ToDomain(MealItemId id, MealItemDocument document) => MealItem.Create(
        id,
        ToOrigin(document),
        document.Quantity,
        Enum.Parse<MeasurementUnit>(document.Unit),
        new Kilocalories(document.Calories),
        new Grams(document.Protein),
        new Grams(document.Carbs),
        new Grams(document.Fat));

    /// <summary>The stored unit name of an item, moved to <paramref name="category"/> with <see cref="MeasurementUnitReclassification"/>.</summary>
    public static string ReclassifyUnit(string storedUnit, MeasurementUnitCategory category) =>
        MeasurementUnitReclassification.Reclassify(Enum.Parse<MeasurementUnit>(storedUnit), category).ToString();

    public static MealItemDocument ToDocument(MealItem item)
    {
        var document = new MealItemDocument
        {
            Quantity = item.Quantity,
            Unit = item.Unit.ToString(),
            Calories = item.Calories.Value,
            Protein = item.Protein.Value,
            Carbs = item.Carbs.Value,
            Fat = item.Fat.Value,
        };

        switch (item.Origin)
        {
            case CatalogFoodOrigin catalogOrigin:
                WriteCatalogOrigin(document, catalogOrigin);
                break;
            case EstimatedFoodOrigin estimatedOrigin:
                WriteEstimatedOrigin(document, estimatedOrigin);
                break;
            default:
                throw new InvalidOperationException($"Unknown meal item origin {item.Origin.GetType().Name}.");
        }

        return document;
    }

    private static MealItemOrigin ToOrigin(MealItemDocument document)
    {
        if (document.Origin == MealItemOriginNames.Estimated)
        {
            return ToEstimatedOrigin(document);
        }

        return new CatalogFoodOrigin(new FoodId(RequireStored(document.FoodId, nameof(document.FoodId))));
    }

    private static EstimatedFoodOrigin ToEstimatedOrigin(MealItemDocument document)
    {
        var nutritionPerBaseUnit = new NutritionAmount(
            new Kilocalories(RequireStored(document.CaloriesPerBaseUnit, nameof(document.CaloriesPerBaseUnit))),
            new Grams(RequireStored(document.ProteinPerBaseUnit, nameof(document.ProteinPerBaseUnit))),
            new Grams(RequireStored(document.CarbsPerBaseUnit, nameof(document.CarbsPerBaseUnit))),
            new Grams(RequireStored(document.FatPerBaseUnit, nameof(document.FatPerBaseUnit))));

        var estimatedFood = EstimatedFood.Create(
            RequireStored(document.Name, nameof(document.Name)),
            Enum.Parse<MeasurementUnit>(RequireStored(document.BaseUnit, nameof(document.BaseUnit))),
            nutritionPerBaseUnit);

        return new EstimatedFoodOrigin(estimatedFood, new CaptureId(RequireStored(document.CaptureId, nameof(document.CaptureId))));
    }

    private static void WriteCatalogOrigin(MealItemDocument document, CatalogFoodOrigin catalogOrigin)
    {
        document.Origin = MealItemOriginNames.Catalog;
        document.FoodId = catalogOrigin.FoodId.Value;
    }

    private static void WriteEstimatedOrigin(MealItemDocument document, EstimatedFoodOrigin estimatedOrigin)
    {
        var estimatedFood = estimatedOrigin.Food;

        document.Origin = MealItemOriginNames.Estimated;
        document.Name = estimatedFood.Name;
        document.CaptureId = estimatedOrigin.CaptureId.Value;
        document.BaseUnit = estimatedFood.BaseUnit.Unit.ToString();
        document.CaloriesPerBaseUnit = estimatedFood.NutritionPerBaseUnit.Calories.Value;
        document.ProteinPerBaseUnit = estimatedFood.NutritionPerBaseUnit.Protein.Value;
        document.CarbsPerBaseUnit = estimatedFood.NutritionPerBaseUnit.Carbs.Value;
        document.FatPerBaseUnit = estimatedFood.NutritionPerBaseUnit.Fat.Value;
    }

    private static string RequireStored(string? value, string fieldName)
    {
        if (value is null)
        {
            throw new InvalidOperationException($"Stored meal item is missing {fieldName}.");
        }

        return value;
    }

    private static TValue RequireStored<TValue>(TValue? value, string fieldName)
        where TValue : struct
    {
        if (value is not { } storedValue)
        {
            throw new InvalidOperationException($"Stored meal item is missing {fieldName}.");
        }

        return storedValue;
    }
}
