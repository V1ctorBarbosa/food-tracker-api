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
            Gender.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500),
            targetWeight);

        // Assert
        user.Id.Should().Be(id);
        user.Name.Should().Be("Victor");
        user.Age.Value.Should().Be(27);
        user.Height.Value.Should().Be(175);
        user.Gender.Should().Be(Gender.Male);
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
    public void Create_SetsOptionalGoals_WhenProvided()
    {
        // Arrange
        var id = new UserId("firebase-uid-123");
        var dailyWaterTarget = new Milliliters(4000);
        var dailyProteinTarget = new Grams(140);
        var dailyCarbsTarget = new Grams(190);
        var dailyFatTarget = new Grams(55);

        // Act
        var user = User.Create(
            id,
            "Victor",
            new Age(27),
            new Centimeters(175),
            Gender.Male,
            ActivityLevel.Moderate,
            new Kilocalories(2000),
            new Kilocalories(500),
            dailyWaterTarget: dailyWaterTarget,
            dailyProteinTarget: dailyProteinTarget,
            dailyCarbsTarget: dailyCarbsTarget,
            dailyFatTarget: dailyFatTarget);

        // Assert
        user.DailyWaterTarget.Should().Be(dailyWaterTarget);
        user.DailyProteinTarget.Should().Be(dailyProteinTarget);
        user.DailyCarbsTarget.Should().Be(dailyCarbsTarget);
        user.DailyFatTarget.Should().Be(dailyFatTarget);
    }

    [Fact]
    public void Create_AllowsNullOptionalGoals()
    {
        // Act
        var user = CreateValidUser();

        // Assert
        user.DailyWaterTarget.Should().BeNull();
        user.DailyProteinTarget.Should().BeNull();
        user.DailyCarbsTarget.Should().BeNull();
        user.DailyFatTarget.Should().BeNull();
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
        Gender.Male,
        ActivityLevel.Moderate,
        new Kilocalories(2000),
        new Kilocalories(500),
        targetWeight);
}
