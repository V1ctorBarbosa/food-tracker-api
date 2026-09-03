using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class CentimetersTest
{
    [Theory]
    [InlineData(50)]
    [InlineData(175)]
    [InlineData(250)]
    public void Constructor_SetsValue_WhenWithinAllowedRange(int value)
    {
        // Act
        var centimeters = new Centimeters(value);

        // Assert
        centimeters.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(251)]
    public void Constructor_Throws_WhenOutsideAllowedRange(int value)
    {
        // Act
        var act = () => new Centimeters(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
