using FluentAssertions;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Tests.Domain.WaterEntries;

public sealed class WaterEntryIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new WaterEntryId("entry-123");

        // Assert
        id.Value.Should().Be("entry-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new WaterEntryId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDistinctIds_OnEachCall()
    {
        // Act
        var first = WaterEntryId.NewId();
        var second = WaterEntryId.NewId();

        // Assert
        first.Should().NotBe(second);
    }
}
