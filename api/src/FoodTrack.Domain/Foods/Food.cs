using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Domain.Foods;

/// <summary>
/// A food available in the local cache of the external nutritional source (docs section 13).
/// Aggregate root for its <see cref="FoodServing"/> children.
/// </summary>
public sealed class Food
{
    public FoodId Id { get; }

    public string Name { get; }

    public FoodSource Source { get; }

    /// <summary>Id of this food in its external source (e.g. a USDA FDC id). Null for custom foods.</summary>
    public string? ExternalId { get; }

    public BaseUnit BaseUnit { get; }

    public Kilocalories CaloriesPerBaseUnit { get; }

    public Grams ProteinPerBaseUnit { get; }

    public Grams CarbsPerBaseUnit { get; }

    public Grams FatPerBaseUnit { get; }

    public IReadOnlyList<FoodServing> Servings { get; }

    /// <summary>Owner of a personal (<see cref="FoodSource.Custom"/>) food. Null for the shared
    /// cache of USDA/Open Food Facts results, which every user can see.</summary>
    public UserId? OwnerId { get; }

    private Food(
        FoodId id,
        string name,
        FoodSource source,
        string? externalId,
        BaseUnit baseUnit,
        Kilocalories caloriesPerBaseUnit,
        Grams proteinPerBaseUnit,
        Grams carbsPerBaseUnit,
        Grams fatPerBaseUnit,
        IReadOnlyList<FoodServing> servings,
        UserId? ownerId)
    {
        Id = id;
        Name = name;
        Source = source;
        ExternalId = externalId;
        BaseUnit = baseUnit;
        CaloriesPerBaseUnit = caloriesPerBaseUnit;
        ProteinPerBaseUnit = proteinPerBaseUnit;
        CarbsPerBaseUnit = carbsPerBaseUnit;
        FatPerBaseUnit = fatPerBaseUnit;
        Servings = servings;
        OwnerId = ownerId;
    }

    /// <summary>Shared foods are visible to everyone; a custom food only to the user who created it.</summary>
    public bool IsVisibleTo(UserId userId) => OwnerId is null || OwnerId == userId;

    /// <summary>
    /// The same food with its base unit moved to <paramref name="unit"/>, keeping the base quantity
    /// and the nutrition values: "42 kcal per 100 g" becomes "42 kcal per 100 ml".
    /// </summary>
    public Food MeasuredIn(MeasurementUnit unit) => Create(
        Id,
        Name,
        Source,
        ExternalId,
        new BaseUnit(BaseUnit.Quantity, unit),
        CaloriesPerBaseUnit,
        ProteinPerBaseUnit,
        CarbsPerBaseUnit,
        FatPerBaseUnit,
        Servings,
        OwnerId);

    public static Food Create(
        FoodId id,
        string name,
        FoodSource source,
        string? externalId,
        BaseUnit baseUnit,
        Kilocalories caloriesPerBaseUnit,
        Grams proteinPerBaseUnit,
        Grams carbsPerBaseUnit,
        Grams fatPerBaseUnit,
        IReadOnlyList<FoodServing>? servings = null,
        UserId? ownerId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new Food(
            id,
            name.Trim(),
            source,
            externalId,
            baseUnit,
            caloriesPerBaseUnit,
            proteinPerBaseUnit,
            carbsPerBaseUnit,
            fatPerBaseUnit,
            servings ?? [],
            ownerId);
    }
}
