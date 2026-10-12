using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Pure mapping between the domain's <see cref="Food"/> aggregate and its Firestore persistence
/// representation. Kept free of any Firestore I/O so it can be unit tested without a live database.
/// </summary>
internal static class FoodDocumentMapper
{
    public static Food ToDomain(FoodId id, FoodDocument document) => Food.Create(
        id,
        document.Name,
        Enum.Parse<FoodSource>(document.Source),
        document.ExternalId,
        new BaseUnit(document.BaseUnitQuantity, Enum.Parse<MeasurementUnit>(document.BaseUnitType)),
        new Kilocalories(document.CaloriesPerBaseUnit),
        new Grams(document.ProteinPerBaseUnit),
        new Grams(document.CarbsPerBaseUnit),
        new Grams(document.FatPerBaseUnit),
        ToOwnerId(document.OwnerId));

    public static FoodDocument ToDocument(Food food) => new()
    {
        Name = food.Name,
        NameLower = food.Name.ToLowerInvariant(),
        Source = food.Source.ToString(),
        ExternalId = food.ExternalId,
        OwnerId = food.OwnerId?.Value,
        BaseUnitQuantity = food.BaseUnit.Quantity,
        BaseUnitType = food.BaseUnit.Unit.ToString(),
        CaloriesPerBaseUnit = food.CaloriesPerBaseUnit.Value,
        ProteinPerBaseUnit = food.ProteinPerBaseUnit.Value,
        CarbsPerBaseUnit = food.CarbsPerBaseUnit.Value,
        FatPerBaseUnit = food.FatPerBaseUnit.Value,
    };

    private static UserId? ToOwnerId(string? ownerId)
    {
        if (ownerId is null)
        {
            return null;
        }

        return new UserId(ownerId);
    }
}
