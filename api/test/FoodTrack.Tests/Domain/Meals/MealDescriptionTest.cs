using FluentAssertions;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class MealDescriptionTest
{
    [Fact]
    public void Constructor_TrimsTheValue()
    {
        // Act
        var description = new MealDescription("  frango grelhado no azeite  ");

        // Assert
        description.Value.Should().Be("frango grelhado no azeite");
    }

    [Fact]
    public void Constructor_AcceptsTheMaximumLength()
    {
        // Arrange
        var value = new string('a', MealDescription.MaximumLength);

        // Act
        var description = new MealDescription(value);

        // Assert
        description.Value.Should().HaveLength(MealDescription.MaximumLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenTheValueIsEmpty(string? value)
    {
        // Act
        var act = () => new MealDescription(value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Throws_WhenTheValueIsLongerThanTheMaximum()
    {
        // Arrange
        var value = new string('a', MealDescription.MaximumLength + 1);

        // Act
        var act = () => new MealDescription(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
