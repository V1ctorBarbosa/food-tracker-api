using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Infrastructure.Users;

namespace FoodTrack.Tests.Infrastructure.Users;

public sealed class UserDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromUser()
    {
        // Arrange
        var user = User.Create(
            new UserId("firebase-uid-123"),
            "Victor",
            new Age(27),
            new Centimeters(175),
            Sex.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500),
            new Kilograms(80));

        // Act
        var document = UserDocumentMapper.ToDocument(user);

        // Assert
        document.Name.Should().Be("Victor");
        document.Age.Should().Be(27);
        document.HeightCentimeters.Should().Be(175);
        document.Sex.Should().Be(nameof(Sex.Male));
        document.ActivityLevel.Should().Be(nameof(ActivityLevel.Moderate));
        document.TargetWeightKilograms.Should().Be(80);
        document.DesiredCalorieDeficit.Should().Be(500);
        document.DailyCalorieTarget.Should().Be(2000);
    }

    [Fact]
    public void ToDocument_MapsNullTargetWeight_AsNull()
    {
        // Arrange
        var user = User.Create(
            new UserId("firebase-uid-123"),
            "Victor",
            new Age(27),
            new Centimeters(175),
            Sex.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500));

        // Act
        var document = UserDocumentMapper.ToDocument(user);

        // Assert
        document.TargetWeightKilograms.Should().BeNull();
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = new UserId("firebase-uid-123");
        var originalUser = User.Create(
            id,
            "Victor",
            new Age(27),
            new Centimeters(175),
            Sex.Female,
            ActivityLevel.Active,
            new Kilocalories(2200),
            new Kilocalories(400),
            new Kilograms(65.5));

        // Act
        var document = UserDocumentMapper.ToDocument(originalUser);
        var roundTrippedUser = UserDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedUser.Id.Should().Be(id);
        roundTrippedUser.Name.Should().Be(originalUser.Name);
        roundTrippedUser.Age.Should().Be(originalUser.Age);
        roundTrippedUser.Height.Should().Be(originalUser.Height);
        roundTrippedUser.Sex.Should().Be(originalUser.Sex);
        roundTrippedUser.ActivityLevel.Should().Be(originalUser.ActivityLevel);
        roundTrippedUser.TargetWeight.Should().Be(originalUser.TargetWeight);
        roundTrippedUser.DesiredCalorieDeficit.Should().Be(originalUser.DesiredCalorieDeficit);
        roundTrippedUser.DailyCalorieTarget.Should().Be(originalUser.DailyCalorieTarget);
    }
}
