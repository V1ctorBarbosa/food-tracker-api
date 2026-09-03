using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class KilogramsTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(80.5)]
    [InlineData(500)]
    public void Constructor_SetsValue_WhenWithinAllowedRange(double value)
    {
        // Act
        var kilograms = new Kilograms(value);

        // Assert
        kilograms.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(500.1)]
    public void Constructor_Throws_WhenOutsideAllowedRange(double value)
    {
        // Act
        var act = () => new Kilograms(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
