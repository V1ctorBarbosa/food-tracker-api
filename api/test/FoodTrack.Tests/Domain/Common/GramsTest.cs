using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class GramsTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(1_000)]
    public void Constructor_SetsValue_WhenWithinAllowedRange(double value)
    {
        // Act
        var grams = new Grams(value);

        // Assert
        grams.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_001)]
    public void Constructor_Throws_WhenOutsideAllowedRange(double value)
    {
        // Act
        var act = () => new Grams(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
