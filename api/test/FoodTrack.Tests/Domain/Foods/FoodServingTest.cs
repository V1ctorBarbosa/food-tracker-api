using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Tests.Domain.Foods;

public sealed class FoodServingTest
{
    [Fact]
    public void Create_ReturnsFoodServing_WithGivenValues()
    {
        // Arrange
        var id = FoodServingId.NewId();

        // Act
        var serving = FoodServing.Create(id, "can", 350, MeasurementUnit.Milliliter);

        // Assert
        serving.Id.Should().Be(id);
        serving.Name.Should().Be("can");
        serving.Quantity.Should().Be(350);
        serving.Unit.Should().Be(MeasurementUnit.Milliliter);
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var serving = FoodServing.Create(FoodServingId.NewId(), "  can  ", 350, MeasurementUnit.Milliliter);

        // Assert
        serving.Name.Should().Be("can");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Act
        var act = () => FoodServing.Create(FoodServingId.NewId(), name, 350, MeasurementUnit.Milliliter);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Throws_WhenQuantityIsNotPositive(double quantity)
    {
        // Act
        var act = () => FoodServing.Create(FoodServingId.NewId(), "can", quantity, MeasurementUnit.Milliliter);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(MeasurementUnit.Kilogram)]
    [InlineData(MeasurementUnit.Liter)]
    public void Create_Throws_WhenUnitIsNotGramOrMilliliter(MeasurementUnit unit)
    {
        // Act
        var act = () => FoodServing.Create(FoodServingId.NewId(), "can", 350, unit);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
