using FluentAssertions;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class CaptureIdTest
{
    [Fact]
    public void Constructor_KeepsTheValue_WhenItIsAGuid()
    {
        // Arrange
        var value = Guid.NewGuid().ToString();

        // Act
        var captureId = new CaptureId(value);

        // Assert
        captureId.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_StoresTheCanonicalForm_WhenTheGuidHasBraces()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var captureId = new CaptureId($"{{{guid}}}");

        // Assert
        captureId.Value.Should().Be(guid.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("../other-user/captures/photo")]
    public void Constructor_Throws_WhenTheValueIsNotAGuid(string value)
    {
        // Act
        var act = () => new CaptureId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_ReturnsDifferentIds()
    {
        // Act
        var firstId = CaptureId.NewId();
        var secondId = CaptureId.NewId();

        // Assert
        firstId.Should().NotBe(secondId);
    }
}
