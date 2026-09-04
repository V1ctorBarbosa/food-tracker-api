using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Foods;

/// <summary>
/// The unit nutritional values are reported per (docs section 16), e.g. "165 kcal per 100g".
/// Always a mass or a volume — never a food-specific serving, and never Kilogram/Liter
/// directly, matching how nutritional data sources normalize values (per 100g or 100ml).
/// </summary>
public readonly record struct BaseUnit
{
    public double Quantity { get; }

    public MeasurementUnit Unit { get; }

    public BaseUnit(double quantity, MeasurementUnit unit)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Base unit quantity must be positive.");
        }

        if (unit is not (MeasurementUnit.Gram or MeasurementUnit.Milliliter))
        {
            throw new ArgumentException($"Base unit must be Gram or Milliliter, got {unit}.", nameof(unit));
        }

        Quantity = quantity;
        Unit = unit;
    }
}
