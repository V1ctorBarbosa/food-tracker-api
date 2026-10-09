namespace FoodTrack.Domain.Common;

/// <summary>
/// Re-reads an amount in the other measurement category, keeping the number and the scale: 350 g
/// becomes 350 ml, 1 kg becomes 1 L. This is not a conversion (see <see cref="MeasurementUnitConversion"/>);
/// it applies when a food is reclassified between food and drink, so amounts already logged for it
/// follow the food's new unit instead of becoming impossible to recalculate.
/// </summary>
public static class MeasurementUnitReclassification
{
    public static MeasurementUnit Reclassify(MeasurementUnit unit, MeasurementUnitCategory targetCategory)
    {
        if (unit.GetCategory() == targetCategory)
        {
            return unit;
        }

        return unit switch
        {
            MeasurementUnit.Gram => MeasurementUnit.Milliliter,
            MeasurementUnit.Kilogram => MeasurementUnit.Liter,
            MeasurementUnit.Milliliter => MeasurementUnit.Gram,
            MeasurementUnit.Liter => MeasurementUnit.Kilogram,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown measurement unit."),
        };
    }
}
