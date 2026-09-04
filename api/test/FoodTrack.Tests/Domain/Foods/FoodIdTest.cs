using FluentAssertions;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Tests.Domain.Foods;

public sealed class FoodIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new FoodId("food-123");

        // Assert
        id.Value.Should().Be("food-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new FoodId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDistinctIds_OnEachCall()
    {
        // Act
        var first = FoodId.NewId();
        var second = FoodId.NewId();

        // Assert
        first.Should().NotBe(second);
    }
}
