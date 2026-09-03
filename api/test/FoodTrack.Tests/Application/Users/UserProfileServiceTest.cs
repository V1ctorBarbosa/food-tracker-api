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
            Sex.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500),
            new Kilograms(80));

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("firebase-uid-123");
        result.Name.Should().Be("Victor");
        result.Age.Should().Be(27);
        result.HeightCentimeters.Should().Be(175);
        result.Sex.Should().Be(nameof(Sex.Male));
        result.ActivityLevel.Should().Be(nameof(ActivityLevel.Moderate));
        result.TargetWeightKilograms.Should().Be(80);
        result.DesiredCalorieDeficit.Should().Be(500);
        result.DailyCalorieTarget.Should().Be(2000);
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
            Sex: "Male",
            ActivityLevel: "Moderate",
            TargetWeightKilograms: 80,
            DesiredCalorieDeficit: 500,
            DailyCalorieTarget: 2000);

        // Act
        var result = await _sut.UpsertProfileAsync(userId, request);

        // Assert
        result.Id.Should().Be("firebase-uid-123");
        result.Name.Should().Be("Victor");
        await _userRepository.Received(1).SaveAsync(
            Arg.Is<User>(u => u.Id == userId && u.Name == "Victor"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertProfileAsync_Throws_AndDoesNotSave_WhenSexIsInvalid()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest(
            Name: "Victor",
            Age: 27,
            HeightCentimeters: 175,
            Sex: "not-a-real-sex",
            ActivityLevel: "Moderate",
            TargetWeightKilograms: 80,
            DesiredCalorieDeficit: 500,
            DailyCalorieTarget: 2000);

        // Act
        var act = async () => await _sut.UpsertProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _userRepository.DidNotReceive().SaveAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
