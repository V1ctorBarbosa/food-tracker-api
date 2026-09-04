using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Tests.Domain.Foods;

public sealed class BaseUnitTest
{
    [Theory]
    [InlineData(100, MeasurementUnit.Gram)]
    [InlineData(100, MeasurementUnit.Milliliter)]
    public void Constructor_SetsValues_WhenValid(double quantity, MeasurementUnit unit)
    {
        // Act
        var baseUnit = new BaseUnit(quantity, unit);

        // Assert
        baseUnit.Quantity.Should().Be(quantity);
        baseUnit.Unit.Should().Be(unit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_Throws_WhenQuantityIsNotPositive(double quantity)
    {
        // Act
        var act = () => new BaseUnit(quantity, MeasurementUnit.Gram);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(MeasurementUnit.Kilogram)]
    [InlineData(MeasurementUnit.Liter)]
    public void Constructor_Throws_WhenUnitIsNotGramOrMilliliter(MeasurementUnit unit)
    {
        // Act
        var act = () => new BaseUnit(100, unit);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
