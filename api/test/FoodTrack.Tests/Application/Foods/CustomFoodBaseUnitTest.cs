using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Tests.Application.Foods;

public sealed class CustomFoodBaseUnitTest
{
    [Theory]
    [InlineData("Gram", MeasurementUnit.Gram)]
    [InlineData("gram", MeasurementUnit.Gram)]
    [InlineData("Milliliter", MeasurementUnit.Milliliter)]
    [InlineData(" MILLILITER ", MeasurementUnit.Milliliter)]
    public void Resolve_ReturnsHundredOfTheRequestedUnit(string requestedUnit, MeasurementUnit expectedUnit)
    {
        // Act
        var baseUnit = CustomFoodBaseUnit.Resolve(requestedUnit, MeasurementUnit.Gram);

        // Assert
        baseUnit.Should().Be(new BaseUnit(100, expectedUnit));
    }

    [Theory]
    [InlineData(MeasurementUnit.Gram)]
    [InlineData(MeasurementUnit.Milliliter)]
    public void Resolve_FallsBackToTheGivenUnit_WhenNoneIsRequested(MeasurementUnit unitWhenMissing)
    {
        // Act
        var baseUnit = CustomFoodBaseUnit.Resolve(null, unitWhenMissing);

        // Assert
        baseUnit.Should().Be(new BaseUnit(100, unitWhenMissing));
    }

    [Theory]
    [InlineData("Kilogram")]
    [InlineData("Liter")]
    [InlineData("0")]
    [InlineData("2")]
    [InlineData("")]
    [InlineData("lata")]
    public void Resolve_Throws_ForAnythingButGramOrMilliliter(string requestedUnit)
    {
        // Act
        var act = () => CustomFoodBaseUnit.Resolve(requestedUnit, MeasurementUnit.Gram);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
