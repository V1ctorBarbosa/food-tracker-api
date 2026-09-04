using FluentAssertions;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class MealItemIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new MealItemId("item-123");

        // Assert
        id.Value.Should().Be("item-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new MealItemId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDistinctIds_OnEachCall()
    {
        // Act
        var first = MealItemId.NewId();
        var second = MealItemId.NewId();

        // Assert
        first.Should().NotBe(second);
    }
}
