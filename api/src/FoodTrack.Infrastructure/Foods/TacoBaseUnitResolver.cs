using FoodTrack.Domain.Common;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Decides whether a TACO food is a food (per 100 g) or a drink (per 100 ml). TACO measures
/// everything per 100 g, drinks included; for the drinks below those values are read as per 100 ml.
/// Drinks weigh between about 0.95 and 1.05 g per ml, so the error stays under 5%.
/// </summary>
internal static class TacoBaseUnitResolver
{
    private const string BeveragesCategory = "Bebidas (alcoólicas e não alcoólicas)";

    // Liquids filed under other categories: juices under "Frutas e derivados", milks under
    // "Leite e derivados". Yogurts stay in grams, as Brazilian labels state them.
    private static readonly IReadOnlySet<int> LiquidFoodIdsOutsideBeverages = new HashSet<int>
    {
        188, // Caju, suco concentrado, envasado
        209, // Laranja, baía, suco
        211, // Laranja, da terra, suco
        213, // Laranja, lima, suco
        215, // Laranja, pêra, suco
        217, // Laranja, valência, suco
        218, // Limão, cravo, suco
        219, // Limão, galego, suco
        234, // Maracujá, suco concentrado, envasado
        252, // Tangerina, Poncã, suco
        258, // Uva, suco concentrado, envasado
        446, // Bebida láctea, pêssego
        454, // Leite, de cabra
        455, // Leite, de vaca, achocolatado
        457, // Leite, de vaca, desnatado, UHT
        458, // Leite, de vaca, integral
        460, // Leite, fermentado
    };

    public static MeasurementUnit Resolve(int tacoId, string? category)
    {
        if (string.Equals(category, BeveragesCategory, StringComparison.Ordinal))
        {
            return MeasurementUnit.Milliliter;
        }

        if (LiquidFoodIdsOutsideBeverages.Contains(tacoId))
        {
            return MeasurementUnit.Milliliter;
        }

        return MeasurementUnit.Gram;
    }
}
