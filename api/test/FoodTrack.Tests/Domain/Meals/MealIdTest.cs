using FluentAssertions;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class MealIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new MealId("meal-123");

        // Assert
        id.Value.Should().Be("meal-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new MealId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDistinctIds_OnEachCall()
    {
        // Act
        var first = MealId.NewId();
        var second = MealId.NewId();

        // Assert
        first.Should().NotBe(second);
    }
}
