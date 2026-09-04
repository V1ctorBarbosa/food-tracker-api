namespace FoodTrack.Domain.Common;

/// <summary>
/// Converts between units of the same measurement category (mass or volume) only.
/// Mass and volume conversion depends on a food's density, so it is never assumed
/// (docs section 18) — converting across categories fails fast instead of guessing.
/// </summary>
public static class MeasurementUnitConversion
{
    private const double GramsPerKilogram = 1_000;
    private const double MillilitersPerLiter = 1_000;

    public static double ConvertTo(double quantity, MeasurementUnit from, MeasurementUnit to)
    {
        if (from == to)
        {
            return quantity;
        }

        return (from, to) switch
        {
            (MeasurementUnit.Kilogram, MeasurementUnit.Gram) => quantity * GramsPerKilogram,
            (MeasurementUnit.Gram, MeasurementUnit.Kilogram) => quantity / GramsPerKilogram,
            (MeasurementUnit.Liter, MeasurementUnit.Milliliter) => quantity * MillilitersPerLiter,
            (MeasurementUnit.Milliliter, MeasurementUnit.Liter) => quantity / MillilitersPerLiter,
            _ => throw new InvalidOperationException(
                $"Cannot convert {from} to {to} — mass and volume are not interchangeable without food-specific density."),
        };
    }
}
