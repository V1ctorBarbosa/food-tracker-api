using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class MeasurementUnitCategoriesTest
{
    [Theory]
    [InlineData(MeasurementUnit.Gram, MeasurementUnitCategory.Mass)]
    [InlineData(MeasurementUnit.Kilogram, MeasurementUnitCategory.Mass)]
    [InlineData(MeasurementUnit.Milliliter, MeasurementUnitCategory.Volume)]
    [InlineData(MeasurementUnit.Liter, MeasurementUnitCategory.Volume)]
    public void GetCategory_ReturnsWhatTheUnitMeasures(MeasurementUnit unit, MeasurementUnitCategory expectedCategory)
    {
        // Act
        var category = unit.GetCategory();

        // Assert
        category.Should().Be(expectedCategory);
    }

    [Fact]
    public void GetCategory_Throws_ForAnUndefinedUnit()
    {
        // Arrange
        var undefinedUnit = (MeasurementUnit)99;

        // Act
        var act = () => undefinedUnit.GetCategory();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
