namespace FoodTrack.Domain.Common;

/// <summary>
/// Mass and volume units a user can log a food/serving quantity in (docs section 15).
/// Mass and volume are never interchangeable without food-specific density — see
/// <see cref="MeasurementUnitConversion"/>.
/// </summary>
public enum MeasurementUnit
{
    Gram,
    Kilogram,
    Milliliter,
    Liter,
}
