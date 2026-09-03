using FluentAssertions;
using FoodTrack.Domain.Common;

namespace FoodTrack.Tests.Domain.Common;

public sealed class AgeTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(27)]
    [InlineData(120)]
    public void Constructor_SetsValue_WhenWithinAllowedRange(int value)
    {
        // Act
        var age = new Age(value);

        // Assert
        age.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    [InlineData(200)]
    public void Constructor_Throws_WhenOutsideAllowedRange(int value)
    {
        // Act
        var act = () => new Age(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
