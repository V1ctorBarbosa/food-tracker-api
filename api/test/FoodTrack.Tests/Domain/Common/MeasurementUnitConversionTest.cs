using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class MeasurementUnitConversionTest
{
    [Theory]
    [InlineData(1, MeasurementUnit.Kilogram, MeasurementUnit.Gram, 1_000)]
    [InlineData(500, MeasurementUnit.Gram, MeasurementUnit.Kilogram, 0.5)]
    [InlineData(1, MeasurementUnit.Liter, MeasurementUnit.Milliliter, 1_000)]
    [InlineData(250, MeasurementUnit.Milliliter, MeasurementUnit.Liter, 0.25)]
    [InlineData(200, MeasurementUnit.Gram, MeasurementUnit.Gram, 200)]
    public void ConvertTo_ReturnsExpectedValue_ForSameCategoryUnits(
        double quantity,
        MeasurementUnit from,
        MeasurementUnit to,
        double expected)
    {
        // Act
        var result = MeasurementUnitConversion.ConvertTo(quantity, from, to);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(MeasurementUnit.Gram, MeasurementUnit.Milliliter)]
    [InlineData(MeasurementUnit.Milliliter, MeasurementUnit.Gram)]
    [InlineData(MeasurementUnit.Kilogram, MeasurementUnit.Liter)]
    public void ConvertTo_Throws_WhenCrossingMassAndVolume(MeasurementUnit from, MeasurementUnit to)
    {
        // Act
        var act = () => MeasurementUnitConversion.ConvertTo(100, from, to);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
