using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class MillilitersTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    [InlineData(5_000)]
    public void Constructor_SetsValue_WhenWithinAllowedRange(int value)
    {
        // Act
        var milliliters = new Milliliters(value);

        // Assert
        milliliters.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5_001)]
    public void Constructor_Throws_WhenOutsideAllowedRange(int value)
    {
        // Act
        var act = () => new Milliliters(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
