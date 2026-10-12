using FluentAssertions;
using FoodTrack.Domain.Meals;
using FoodTrack.Infrastructure.Meals;

namespace FoodTrack.Tests.Infrastructure.Meals;

public sealed class MealCaptureDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields()
    {
        // Arrange
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, new MealDescription("o frango é empanado"));

        // Act
        var document = MealCaptureDocumentMapper.ToDocument(capture);

        // Assert
        document.Id.Should().Be(capture.Id.Value);
        document.HasPhoto.Should().BeTrue();
        document.Description.Should().Be("o frango é empanado");
    }

    [Fact]
    public void ToDocument_LeavesTheDescriptionEmpty_WhenThereIsNone()
    {
        // Arrange
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, description: null);

        // Act
        var document = MealCaptureDocumentMapper.ToDocument(capture);

        // Assert
        document.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "frito")]
    [InlineData(false, "200 g de arroz integral")]
    public void ToDomain_RoundTrips_ThroughToDocument(bool hasPhoto, string? descriptionText)
    {
        // Arrange
        MealDescription? description = null;
        if (descriptionText is not null)
        {
            description = new MealDescription(descriptionText);
        }

        var originalCapture = MealCapture.Create(CaptureId.NewId(), hasPhoto, description);

        // Act
        var roundTrippedCapture = MealCaptureDocumentMapper.ToDomain(MealCaptureDocumentMapper.ToDocument(originalCapture));

        // Assert
        roundTrippedCapture.Should().Be(originalCapture);
    }
}
