using System.Text.RegularExpressions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Decides whether an Open Food Facts product is a food (per 100 g) or a drink (per 100 ml).
/// Open Food Facts keeps a drink's nutrients per 100 ml even in its "_100g" fields, so the values
/// are taken as they come; only the unit they are read in changes.
/// The search endpoint does not return the structured "nutrition_data_per" or "product_quantity_unit",
/// so the unit is read from the free-text package quantity ("350ml", "1 L", "6 x 350 ml", "500 g"),
/// falling back to the beverages category when that text has no unit.
/// </summary>
internal static partial class OpenFoodFactsBaseUnitResolver
{
    private const string BeveragesCategoryTag = "en:beverages";
    private const string UnitGroupName = "unit";

    private static readonly IReadOnlyDictionary<string, MeasurementUnit> UnitsBySymbol =
        new Dictionary<string, MeasurementUnit>(StringComparer.OrdinalIgnoreCase)
        {
            ["ml"] = MeasurementUnit.Milliliter,
            ["cl"] = MeasurementUnit.Milliliter,
            ["l"] = MeasurementUnit.Milliliter,
            ["g"] = MeasurementUnit.Gram,
            ["kg"] = MeasurementUnit.Gram,
            ["mg"] = MeasurementUnit.Gram,
        };

    public static MeasurementUnit Resolve(string? quantity, IReadOnlyCollection<string> categoryTags)
    {
        var unitFromQuantity = ReadUnitFromQuantity(quantity);
        if (unitFromQuantity is { } unit)
        {
            return unit;
        }

        if (categoryTags.Contains(BeveragesCategoryTag, StringComparer.OrdinalIgnoreCase))
        {
            return MeasurementUnit.Milliliter;
        }

        return MeasurementUnit.Gram;
    }

    private static MeasurementUnit? ReadUnitFromQuantity(string? quantity)
    {
        if (string.IsNullOrWhiteSpace(quantity))
        {
            return null;
        }

        var match = QuantityUnitPattern().Match(quantity);
        if (!match.Success)
        {
            return null;
        }

        return UnitsBySymbol[match.Groups[UnitGroupName].Value];
    }

    // A unit symbol right after a number, and not the start of a longer word ("1 lata" is not litres).
    [GeneratedRegex(@"\d\s*(?<unit>ml|cl|kg|mg|l|g)(?![a-z])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QuantityUnitPattern();
}
