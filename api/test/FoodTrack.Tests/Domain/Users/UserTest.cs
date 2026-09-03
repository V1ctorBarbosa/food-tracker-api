using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Tests.Domain.Users;

public sealed class UserTest
{
    [Fact]
    public void Create_ReturnsUser_WithGivenValues()
    {
        // Arrange
        var id = new UserId("firebase-uid-123");
        var targetWeight = new Kilograms(80);

        // Act
        var user = User.Create(
            id,
            "Victor",
            new Age(27),
            new Centimeters(175),
            Sex.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500),
            targetWeight);

        // Assert
        user.Id.Should().Be(id);
        user.Name.Should().Be("Victor");
        user.Age.Value.Should().Be(27);
        user.Height.Value.Should().Be(175);
        user.Sex.Should().Be(Sex.Male);
        user.ActivityLevel.Should().Be(ActivityLevel.Moderate);
        user.DailyCalorieTarget.Value.Should().Be(2000);
        user.DesiredCalorieDeficit.Value.Should().Be(500);
        user.TargetWeight.Should().Be(targetWeight);
    }

    [Fact]
    public void Create_AllowsNullTargetWeight()
    {
        // Act
        var user = CreateValidUser(targetWeight: null);

        // Assert
        user.TargetWeight.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var user = CreateValidUser(name: "  Victor  ");

        // Assert
        user.Name.Should().Be("Victor");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Act
        var act = () => CreateValidUser(name: name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static User CreateValidUser(string name = "Victor", Kilograms? targetWeight = null) => User.Create(
        new UserId("firebase-uid-123"),
        name,
        new Age(27),
        new Centimeters(175),
        Sex.Male,
        ActivityLevel.Moderate,
        new Kilocalories(2000),
        new Kilocalories(500),
        targetWeight);
}
