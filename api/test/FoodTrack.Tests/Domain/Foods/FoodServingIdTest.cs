using FluentAssertions;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Tests.Domain.Foods;

public sealed class FoodServingIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new FoodServingId("serving-123");

        // Assert
        id.Value.Should().Be("serving-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new FoodServingId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDistinctIds_OnEachCall()
    {
        // Act
        var first = FoodServingId.NewId();
        var second = FoodServingId.NewId();

        // Assert
        first.Should().NotBe(second);
    }
}
