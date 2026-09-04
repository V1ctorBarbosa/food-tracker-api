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
