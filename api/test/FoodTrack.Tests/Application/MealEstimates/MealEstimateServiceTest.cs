using FluentAssertions;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using NSubstitute;

namespace FoodTrack.Tests.Application.MealEstimates;

public sealed class MealEstimateServiceTest
{
    private static readonly Uri PendingPhotoUrl = new("https://photos.example.test/pending/photo.jpg");
    private static readonly RecognizedFood GrilledChicken = new("Peito de frango grelhado", 150, "Gram", 160, 32, 0, 3);
    private static readonly RecognizedFood WhiteRice = new("Arroz branco cozido", 200, "Gram", 128, 2.5, 28.1, 0.2);

    private readonly IMealEstimator _mealEstimator = Substitute.For<IMealEstimator>();
    private readonly IMealPhotoStorage _mealPhotoStorage = Substitute.For<IMealPhotoStorage>();
    private readonly IMealEstimateAccessPolicy _accessPolicy = Substitute.For<IMealEstimateAccessPolicy>();
    private readonly IMealEstimateQuota _quota = Substitute.For<IMealEstimateQuota>();
    private readonly MealEstimateService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public MealEstimateServiceTest()
    {
        _accessPolicy.CanEstimate(_userId).Returns(true);
        _mealPhotoStorage.GetStateAsync(_userId, Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(StoredPhotoState.Pending);
        _mealPhotoStorage.GetPendingPhotoReadUrlAsync(_userId, Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(PendingPhotoUrl);
        _mealEstimator.EstimateAsync(Arg.Any<MealEstimateInput>(), Arg.Any<CancellationToken>()).Returns(new RecognizedMeal([GrilledChicken, WhiteRice]));
        _quota.TryConsumeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new MealEstimateService(_mealEstimator, _mealPhotoStorage, _accessPolicy, _quota);
    }

    [Fact]
    public async Task CreateUploadSlotAsync_ReturnsASlotForANewCapture()
    {
        // Arrange
        var uploadUrl = new Uri("https://photos.example.test/pending/upload");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var headers = new Dictionary<string, string> { ["Content-Type"] = "image/jpeg" };
        _mealPhotoStorage.CreateUploadSlotAsync(_userId, Arg.Any<CaptureId>(), Arg.Any<CancellationToken>())
            .Returns(new PhotoUploadSlot(uploadUrl, headers, expiresAt));

        // Act
        var slot = await _sut.CreateUploadSlotAsync(_userId);

        // Assert
        Guid.TryParse(slot.CaptureId, out _).Should().BeTrue();
        slot.UploadUrl.Should().Be(uploadUrl.ToString());
        slot.Headers.Should().BeEquivalentTo(headers);
        slot.ExpiresAt.Should().Be(expiresAt);
        await _mealPhotoStorage.Received(1).CreateUploadSlotAsync(_userId, new CaptureId(slot.CaptureId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUploadSlotAsync_Throws_WhenTheUserMayNotEstimate()
    {
        // Arrange
        _accessPolicy.CanEstimate(_userId).Returns(false);

        // Act
        var act = () => _sut.CreateUploadSlotAsync(_userId);

        // Assert
        await act.Should().ThrowAsync<MealEstimateAccessDeniedException>();
        await _mealPhotoStorage.DidNotReceiveWithAnyArgs().CreateUploadSlotAsync(default, default);
    }

    [Fact]
    public async Task EstimateAsync_Throws_WithoutCallingTheEstimator_WhenTheUserMayNotEstimate()
    {
        // Arrange
        _accessPolicy.CanEstimate(_userId).Returns(false);

        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(null, "um prato de feijoada com farofa"));

        // Assert
        await act.Should().ThrowAsync<MealEstimateAccessDeniedException>();
        await _mealEstimator.DidNotReceiveWithAnyArgs().EstimateAsync(default!, default);
    }

    [Fact]
    public async Task EstimateAsync_SendsThePhotoAndTheDescription_ForAPhotoEstimate()
    {
        // Arrange
        var captureId = CaptureId.NewId();

        // Act
        var estimate = await _sut.EstimateAsync(_userId, new EstimateMealRequest(captureId.Value, "o frango é empanado"));

        // Assert
        estimate.CaptureId.Should().Be(captureId.Value);
        estimate.HasPhoto.Should().BeTrue();
        estimate.Description.Should().Be("o frango é empanado");
        await _mealEstimator.Received(1).EstimateAsync(
            new MealEstimateInput(PendingPhotoUrl, new MealDescription("o frango é empanado")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EstimateAsync_SendsThePhotoAlone_WhenThereIsNoDescription()
    {
        // Arrange
        var captureId = CaptureId.NewId();

        // Act
        await _sut.EstimateAsync(_userId, new EstimateMealRequest(captureId.Value, "   "));

        // Assert
        await _mealEstimator.Received(1).EstimateAsync(new MealEstimateInput(PendingPhotoUrl, null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EstimateAsync_MintsACapture_AndNeverTouchesStorage_ForADescriptionOnlyEstimate()
    {
        // Act
        var estimate = await _sut.EstimateAsync(_userId, new EstimateMealRequest(null, "200 g de arroz integral"));

        // Assert
        Guid.TryParse(estimate.CaptureId, out _).Should().BeTrue();
        estimate.HasPhoto.Should().BeFalse();
        await _mealEstimator.Received(1).EstimateAsync(
            new MealEstimateInput(null, new MealDescription("200 g de arroz integral")),
            Arg.Any<CancellationToken>());
        await _mealPhotoStorage.DidNotReceiveWithAnyArgs().GetStateAsync(default, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("almoço")]
    public async Task EstimateAsync_Throws_WithoutCallingTheEstimator_WhenADescriptionOnlyEstimateIsTooShort(string? description)
    {
        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(null, description));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _mealEstimator.DidNotReceiveWithAnyArgs().EstimateAsync(default!, default);
    }

    [Theory]
    [InlineData(StoredPhotoState.Missing)]
    [InlineData(StoredPhotoState.TooLarge)]
    [InlineData(StoredPhotoState.Attached)]
    public async Task EstimateAsync_Throws_WithoutCallingTheEstimator_WhenThePhotoCannotBeEstimated(StoredPhotoState photoState)
    {
        // Arrange
        var captureId = CaptureId.NewId();
        _mealPhotoStorage.GetStateAsync(_userId, captureId, Arg.Any<CancellationToken>()).Returns(photoState);

        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(captureId.Value, null));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _mealEstimator.DidNotReceiveWithAnyArgs().EstimateAsync(default!, default);
    }

    [Fact]
    public async Task EstimateAsync_Throws_WhenTheCaptureIdIsNotAGuid()
    {
        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest("../other-user/photo", null));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EstimateAsync_ReturnsItemsReadyToBeSavedInAMeal()
    {
        // Arrange
        var captureId = CaptureId.NewId();

        // Act
        var estimate = await _sut.EstimateAsync(_userId, new EstimateMealRequest(captureId.Value, null));

        // Assert
        estimate.Items.Should().HaveCount(2);
        var chicken = estimate.Items[0];
        chicken.Quantity.Should().Be(150);
        chicken.Unit.Should().Be("Gram");
        chicken.Calories.Should().Be(240);
        chicken.EstimatedFood.Name.Should().Be("Peito de frango grelhado");
        chicken.EstimatedFood.CaptureId.Should().Be(captureId.Value);
        chicken.EstimatedFood.CaloriesPerBaseUnit.Should().Be(160);
    }

    [Fact]
    public async Task EstimateAsync_Throws_WithoutCallingTheEstimator_WhenTheMonthlyLimitIsReached()
    {
        // Arrange
        _quota.TryConsumeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(CaptureId.NewId().Value, null));

        // Assert
        await act.Should().ThrowAsync<MonthlyEstimateLimitReachedException>();
        await _mealEstimator.DidNotReceiveWithAnyArgs().EstimateAsync(default!, default);
    }

    [Fact]
    public async Task EstimateAsync_CountsOneEstimate_ForEachModelCall()
    {
        // Act
        await _sut.EstimateAsync(_userId, new EstimateMealRequest(null, "200 g de arroz integral"));

        // Assert
        await _quota.Received(1).TryConsumeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EstimateAsync_DoesNotCount_ARequestRejectedBeforeTheModelCall()
    {
        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(null, "almoço"));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _quota.DidNotReceiveWithAnyArgs().TryConsumeAsync(default, default);
    }

    [Fact]
    public async Task EstimateAsync_DoesNotCount_WhenTheUserMayNotEstimate()
    {
        // Arrange
        _accessPolicy.CanEstimate(_userId).Returns(false);

        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(null, "200 g de arroz integral"));

        // Assert
        await act.Should().ThrowAsync<MealEstimateAccessDeniedException>();
        await _quota.DidNotReceiveWithAnyArgs().TryConsumeAsync(default, default);
    }

    [Fact]
    public async Task EstimateAsync_Throws_WhenNoFoodIsRecognized()
    {
        // Arrange
        _mealEstimator.EstimateAsync(Arg.Any<MealEstimateInput>(), Arg.Any<CancellationToken>()).Returns(new RecognizedMeal([]));

        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(CaptureId.NewId().Value, null));

        // Assert
        await act.Should().ThrowAsync<NoFoodRecognizedException>();
    }

    [Fact]
    public async Task EstimateAsync_DropsImpossibleFoods_AndKeepsTheRest()
    {
        // Arrange
        var impossibleFood = GrilledChicken with { CaloriesPerBaseUnit = 5_000 };
        _mealEstimator.EstimateAsync(Arg.Any<MealEstimateInput>(), Arg.Any<CancellationToken>()).Returns(new RecognizedMeal([impossibleFood, WhiteRice]));

        // Act
        var estimate = await _sut.EstimateAsync(_userId, new EstimateMealRequest(CaptureId.NewId().Value, null));

        // Assert
        estimate.Items.Should().ContainSingle().Which.EstimatedFood.Name.Should().Be("Arroz branco cozido");
    }

    [Fact]
    public async Task EstimateAsync_Throws_WhenEveryRecognizedFoodIsImpossible()
    {
        // Arrange
        var impossibleFood = GrilledChicken with { Quantity = -10 };
        _mealEstimator.EstimateAsync(Arg.Any<MealEstimateInput>(), Arg.Any<CancellationToken>()).Returns(new RecognizedMeal([impossibleFood]));

        // Act
        var act = () => _sut.EstimateAsync(_userId, new EstimateMealRequest(CaptureId.NewId().Value, null));

        // Assert
        await act.Should().ThrowAsync<MealEstimatorUnavailableException>();
    }
}
