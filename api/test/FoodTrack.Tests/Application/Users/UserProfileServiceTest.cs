using FluentAssertions;
using FoodTrack.Application.Users;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using NSubstitute;

namespace FoodTrack.Tests.Application.Users;

public sealed class UserProfileServiceTest
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly UserProfileService _sut;

    public UserProfileServiceTest()
    {
        _sut = new UserProfileService(_userRepository);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsMappedDto_WhenUserExists()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var user = User.Create(
            userId,
            "Victor",
            new Age(27),
            new Centimeters(175),
            Gender.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500),
            new Kilograms(80),
            new Milliliters(4000),
            new Grams(140),
            new Grams(190),
            new Grams(55));

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("firebase-uid-123");
        result.Name.Should().Be("Victor");
        result.Age.Should().Be(27);
        result.HeightCentimeters.Should().Be(175);
        result.Gender.Should().Be(nameof(Gender.Male));
        result.ActivityLevel.Should().Be(nameof(ActivityLevel.Moderate));
        result.TargetWeightKilograms.Should().Be(80);
        result.DesiredCalorieDeficit.Should().Be(500);
        result.DailyCalorieTarget.Should().Be(2000);
        result.DailyWaterTargetMilliliters.Should().Be(4000);
        result.DailyProteinTargetGrams.Should().Be(140);
        result.DailyCarbsTargetGrams.Should().Be(190);
        result.DailyFatTargetGrams.Should().Be(55);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNullTargets_WhenUserHasNoOptionalGoalsSet()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var user = User.Create(
            userId,
            "Victor",
            new Age(27),
            new Centimeters(175),
            Gender.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500));

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.TargetWeightKilograms.Should().BeNull();
        result.DailyWaterTargetMilliliters.Should().BeNull();
        result.DailyProteinTargetGrams.Should().BeNull();
        result.DailyCarbsTargetGrams.Should().BeNull();
        result.DailyFatTargetGrams.Should().BeNull();
    }

    [Fact]
    public async Task UpsertProfileAsync_SavesUser_WithValuesFromRequest()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest(
            Name: "Victor",
            Age: 27,
            HeightCentimeters: 175,
            Gender: "Male",
            ActivityLevel: "Moderate",
            TargetWeightKilograms: 80,
            DesiredCalorieDeficit: 500,
            DailyCalorieTarget: 2000,
            DailyWaterTargetMilliliters: 4000,
            DailyProteinTargetGrams: 140,
            DailyCarbsTargetGrams: 190,
            DailyFatTargetGrams: 55);

        // Act
        var result = await _sut.UpsertProfileAsync(userId, request);

        // Assert
        result.Id.Should().Be("firebase-uid-123");
        result.Name.Should().Be("Victor");
        result.DailyWaterTargetMilliliters.Should().Be(4000);
        result.DailyProteinTargetGrams.Should().Be(140);
        result.DailyCarbsTargetGrams.Should().Be(190);
        result.DailyFatTargetGrams.Should().Be(55);
        await _userRepository.Received(1).SaveAsync(
            Arg.Is<User>(user => user.Id == userId && user.Name == "Victor"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Male")]
    [InlineData("Female")]
    [InlineData("TransMale")]
    [InlineData("TransFemale")]
    [InlineData("Nonbinary")]
    [InlineData("Other")]
    public async Task UpsertProfileAsync_SavesUser_ForEveryGenderOption(string gender)
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest(
            Name: "Victor",
            Age: 27,
            HeightCentimeters: 175,
            Gender: gender,
            ActivityLevel: "Moderate",
            TargetWeightKilograms: null,
            DesiredCalorieDeficit: 500,
            DailyCalorieTarget: 2000,
            DailyWaterTargetMilliliters: null,
            DailyProteinTargetGrams: null,
            DailyCarbsTargetGrams: null,
            DailyFatTargetGrams: null);

        // Act
        var result = await _sut.UpsertProfileAsync(userId, request);

        // Assert
        result.Gender.Should().Be(gender);
    }

    [Fact]
    public async Task UpsertProfileAsync_SavesUser_WithNullGoals_WhenOptionalTargetsAreOmitted()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest(
            Name: "Victor",
            Age: 27,
            HeightCentimeters: 175,
            Gender: "Male",
            ActivityLevel: "Moderate",
            TargetWeightKilograms: null,
            DesiredCalorieDeficit: 500,
            DailyCalorieTarget: 2000,
            DailyWaterTargetMilliliters: null,
            DailyProteinTargetGrams: null,
            DailyCarbsTargetGrams: null,
            DailyFatTargetGrams: null);

        // Act
        var result = await _sut.UpsertProfileAsync(userId, request);

        // Assert
        result.DailyWaterTargetMilliliters.Should().BeNull();
        result.DailyProteinTargetGrams.Should().BeNull();
        result.DailyCarbsTargetGrams.Should().BeNull();
        result.DailyFatTargetGrams.Should().BeNull();
    }

    [Fact]
    public async Task UpsertProfileAsync_Throws_AndDoesNotSave_WhenGenderIsInvalid()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest(
            Name: "Victor",
            Age: 27,
            HeightCentimeters: 175,
            Gender: "not-a-real-gender",
            ActivityLevel: "Moderate",
            TargetWeightKilograms: 80,
            DesiredCalorieDeficit: 500,
            DailyCalorieTarget: 2000,
            DailyWaterTargetMilliliters: null,
            DailyProteinTargetGrams: null,
            DailyCarbsTargetGrams: null,
            DailyFatTargetGrams: null);

        // Act
        var act = async () => await _sut.UpsertProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _userRepository.DidNotReceive().SaveAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
