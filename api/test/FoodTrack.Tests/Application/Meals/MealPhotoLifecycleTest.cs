using FluentAssertions;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using NSubstitute;

namespace FoodTrack.Tests.Application.Meals;

public sealed class MealPhotoLifecycleTest
{
    private readonly IMealPhotoStorage _mealPhotoStorage = Substitute.For<IMealPhotoStorage>();
    private readonly MealPhotoLifecycle _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public MealPhotoLifecycleTest()
    {
        _sut = new MealPhotoLifecycle(_mealPhotoStorage);
    }

    [Fact]
    public void FindNewCapturesWithPhoto_ReturnsOnlyPhotoCapturesThatWereNotThereBefore()
    {
        // Arrange
        var keptCapture = CreatePhotoCapture();
        var newCapture = CreatePhotoCapture();
        var newDescriptionCapture = CreateDescriptionCapture();

        // Act
        var newCaptures = MealPhotoLifecycle.FindNewCapturesWithPhoto([keptCapture], [keptCapture, newCapture, newDescriptionCapture]);

        // Assert
        newCaptures.Should().Equal(newCapture);
    }

    [Fact]
    public void FindRemovedCapturesWithPhoto_ReturnsOnlyPhotoCapturesThatAreGone()
    {
        // Arrange
        var keptCapture = CreatePhotoCapture();
        var removedCapture = CreatePhotoCapture();
        var removedDescriptionCapture = CreateDescriptionCapture();

        // Act
        var removedCaptures = MealPhotoLifecycle.FindRemovedCapturesWithPhoto([keptCapture, removedCapture, removedDescriptionCapture], [keptCapture]);

        // Assert
        removedCaptures.Should().Equal(removedCapture);
    }

    [Theory]
    [InlineData(StoredPhotoState.Pending)]
    [InlineData(StoredPhotoState.Attached)]
    public async Task EnsureNewPhotosAreUploadedAsync_Passes_WhenThePhotoIsStored(StoredPhotoState photoState)
    {
        // Arrange
        var capture = CreatePhotoCapture();
        _mealPhotoStorage.GetStateAsync(_userId, capture.Id, Arg.Any<CancellationToken>()).Returns(photoState);

        // Act
        var act = () => _sut.EnsureNewPhotosAreUploadedAsync(_userId, [], [capture]);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(StoredPhotoState.Missing)]
    [InlineData(StoredPhotoState.TooLarge)]
    public async Task EnsureNewPhotosAreUploadedAsync_Throws_WhenThePhotoIsNotUsable(StoredPhotoState photoState)
    {
        // Arrange
        var capture = CreatePhotoCapture();
        _mealPhotoStorage.GetStateAsync(_userId, capture.Id, Arg.Any<CancellationToken>()).Returns(photoState);

        // Act
        var act = () => _sut.EnsureNewPhotosAreUploadedAsync(_userId, [], [capture]);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SyncAfterSaveAsync_AttachesNewPhotos_AndDeletesRemovedOnes()
    {
        // Arrange
        var keptCapture = CreatePhotoCapture();
        var newCapture = CreatePhotoCapture();
        var removedCapture = CreatePhotoCapture();

        // Act
        await _sut.SyncAfterSaveAsync(_userId, [keptCapture, removedCapture], [keptCapture, newCapture]);

        // Assert
        await _mealPhotoStorage.Received(1).MarkAttachedAsync(_userId, newCapture.Id, Arg.Any<CancellationToken>());
        await _mealPhotoStorage.Received(1).DeleteAsync(_userId, removedCapture.Id, Arg.Any<CancellationToken>());
        await _mealPhotoStorage.DidNotReceive().MarkAttachedAsync(_userId, keptCapture.Id, Arg.Any<CancellationToken>());
        await _mealPhotoStorage.DidNotReceive().DeleteAsync(_userId, keptCapture.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetReadUrlsAsync_ReturnsAUrlForEachCaptureWithAPhoto()
    {
        // Arrange
        var photoCapture = CreatePhotoCapture();
        var descriptionCapture = CreateDescriptionCapture();
        var photoReadUrl = new Uri("https://photos.example.test/attached/photo.jpg");
        _mealPhotoStorage.GetAttachedPhotoReadUrlAsync(_userId, photoCapture.Id, Arg.Any<CancellationToken>()).Returns(photoReadUrl);
        var meal = CreateMeal([photoCapture, descriptionCapture]);

        // Act
        var readUrls = await _sut.GetReadUrlsAsync(meal);

        // Assert
        readUrls.Should().HaveCount(1).And.ContainKey(photoCapture.Id).WhoseValue.Should().Be(photoReadUrl);
    }

    [Fact]
    public async Task DeletePhotosOfAsync_DeletesOnlyCapturesWithAPhoto()
    {
        // Arrange
        var photoCapture = CreatePhotoCapture();
        var descriptionCapture = CreateDescriptionCapture();
        var meal = CreateMeal([photoCapture, descriptionCapture]);

        // Act
        await _sut.DeletePhotosOfAsync(meal);

        // Assert
        await _mealPhotoStorage.Received(1).DeleteAsync(_userId, photoCapture.Id, Arg.Any<CancellationToken>());
        await _mealPhotoStorage.DidNotReceive().DeleteAsync(_userId, descriptionCapture.Id, Arg.Any<CancellationToken>());
    }

    private Meal CreateMeal(IReadOnlyList<MealCapture> captures) => Meal.Create(
        MealId.NewId(),
        _userId,
        DateTimeOffset.UtcNow,
        MealType.Lunch,
        [MealItem.Create(MealItemId.NewId(), FoodId.NewId(), 100, MeasurementUnit.Gram, new Kilocalories(100), new Grams(1), new Grams(1), new Grams(1))],
        captures);

    private static MealCapture CreatePhotoCapture() => MealCapture.Create(CaptureId.NewId(), hasPhoto: true, description: null);

    private static MealCapture CreateDescriptionCapture() =>
        MealCapture.Create(CaptureId.NewId(), hasPhoto: false, new MealDescription("um copo de suco de laranja"));
}
