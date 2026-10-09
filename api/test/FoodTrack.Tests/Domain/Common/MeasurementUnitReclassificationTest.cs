using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class MeasurementUnitReclassificationTest
{
    [Theory]
    [InlineData(MeasurementUnit.Gram, MeasurementUnitCategory.Volume, MeasurementUnit.Milliliter)]
    [InlineData(MeasurementUnit.Kilogram, MeasurementUnitCategory.Volume, MeasurementUnit.Liter)]
    [InlineData(MeasurementUnit.Milliliter, MeasurementUnitCategory.Mass, MeasurementUnit.Gram)]
    [InlineData(MeasurementUnit.Liter, MeasurementUnitCategory.Mass, MeasurementUnit.Kilogram)]
    public void Reclassify_KeepsTheScale_WhenMovingToTheOtherCategory(
        MeasurementUnit unit,
        MeasurementUnitCategory targetCategory,
        MeasurementUnit expectedUnit)
    {
        // Act
        var reclassifiedUnit = MeasurementUnitReclassification.Reclassify(unit, targetCategory);

        // Assert
        reclassifiedUnit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData(MeasurementUnit.Gram, MeasurementUnitCategory.Mass)]
    [InlineData(MeasurementUnit.Kilogram, MeasurementUnitCategory.Mass)]
    [InlineData(MeasurementUnit.Milliliter, MeasurementUnitCategory.Volume)]
    [InlineData(MeasurementUnit.Liter, MeasurementUnitCategory.Volume)]
    public void Reclassify_ReturnsTheSameUnit_WhenItIsAlreadyInTheTargetCategory(
        MeasurementUnit unit,
        MeasurementUnitCategory targetCategory)
    {
        // Act
        var reclassifiedUnit = MeasurementUnitReclassification.Reclassify(unit, targetCategory);

        // Assert
        reclassifiedUnit.Should().Be(unit);
    }
}
