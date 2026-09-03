using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class KilocaloriesTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(2000)]
    [InlineData(10_000)]
    public void Constructor_SetsValue_WhenWithinAllowedRange(int value)
    {
        // Act
        var kilocalories = new Kilocalories(value);

        // Assert
        kilocalories.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void Constructor_Throws_WhenOutsideAllowedRange(int value)
    {
        // Act
        var act = () => new Kilocalories(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
