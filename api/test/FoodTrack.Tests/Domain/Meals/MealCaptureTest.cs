using FluentAssertions;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class MealCaptureTest
{
    [Fact]
    public void Create_ReturnsCapture_WithAPhotoAndNoDescription()
    {
        // Arrange
        var id = CaptureId.NewId();

        // Act
        var capture = MealCapture.Create(id, hasPhoto: true, description: null);

        // Assert
        capture.Id.Should().Be(id);
        capture.HasPhoto.Should().BeTrue();
        capture.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ReturnsCapture_WithAPhotoAndAShortDescription()
    {
        // Act
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, new MealDescription("frito"));

        // Assert
        capture.Description.Should().Be(new MealDescription("frito"));
    }

    [Fact]
    public void Create_ReturnsCapture_WithoutAPhoto_WhenTheDescriptionIsLongEnough()
    {
        // Arrange
        var description = new MealDescription("200 g de arroz integral");

        // Act
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: false, description);

        // Assert
        capture.HasPhoto.Should().BeFalse();
        capture.Description.Should().Be(description);
    }

    [Fact]
    public void Create_Throws_WithoutAPhotoOrADescription()
    {
        // Act
        var act = () => MealCapture.Create(CaptureId.NewId(), hasPhoto: false, description: null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("almoço")]
    [InlineData("123456789")]
    public void Create_Throws_WithoutAPhoto_WhenTheDescriptionIsTooShort(string text)
    {
        // Act
        var act = () => MealCapture.Create(CaptureId.NewId(), hasPhoto: false, new MealDescription(text));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_AcceptsADescriptionOfExactlyTheMinimumLength_WithoutAPhoto()
    {
        // Arrange
        var text = new string('a', MealCapture.MinimumDescriptionLengthWithoutPhoto);

        // Act
        var act = () => MealCapture.Create(CaptureId.NewId(), hasPhoto: false, new MealDescription(text));

        // Assert
        act.Should().NotThrow();
    }
}
