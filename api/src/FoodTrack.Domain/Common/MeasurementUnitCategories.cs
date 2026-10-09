namespace FoodTrack.Domain.Common;

public static class MeasurementUnitCategories
{
    public static MeasurementUnitCategory GetCategory(this MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Gram or MeasurementUnit.Kilogram => MeasurementUnitCategory.Mass,
        MeasurementUnit.Milliliter or MeasurementUnit.Liter => MeasurementUnitCategory.Volume,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown measurement unit."),
    };
}
