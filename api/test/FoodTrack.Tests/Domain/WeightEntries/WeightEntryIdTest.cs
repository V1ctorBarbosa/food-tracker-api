using FluentAssertions;
using FoodTrack.Domain.WeightEntries;

namespace FoodTrack.Tests.Domain.WeightEntries;

public sealed class WeightEntryIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new WeightEntryId("entry-123");

        // Assert
        id.Value.Should().Be("entry-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new WeightEntryId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDistinctIds_OnEachCall()
    {
        // Act
        var first = WeightEntryId.NewId();
        var second = WeightEntryId.NewId();

        // Assert
        first.Should().NotBe(second);
    }
}
