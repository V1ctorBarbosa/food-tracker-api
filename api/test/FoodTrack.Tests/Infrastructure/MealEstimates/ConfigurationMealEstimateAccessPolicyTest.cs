using FluentAssertions;
using FoodTrack.Domain.Users;
using FoodTrack.Infrastructure.MealEstimates;
using Microsoft.Extensions.Options;

namespace FoodTrack.Tests.Infrastructure.MealEstimates;

public sealed class ConfigurationMealEstimateAccessPolicyTest
{
    [Theory]
    [InlineData("firebase-uid-123", "firebase-uid-123", true)]
    [InlineData(" firebase-uid-123 , firebase-uid-456 ", "firebase-uid-456", true)]
    [InlineData("firebase-uid-123", "firebase-uid-456", false)]
    [InlineData("firebase-uid-123", "FIREBASE-UID-123", false)]
    [InlineData("", "firebase-uid-123", false)]
    public void CanEstimate_AllowsOnlyTheConfiguredUsers(string allowedUserIds, string userId, bool expectedCanEstimate)
    {
        // Arrange
        var sut = new ConfigurationMealEstimateAccessPolicy(Options.Create(new MealEstimateAccessOptions { AllowedUserIds = allowedUserIds }));

        // Act
        var canEstimate = sut.CanEstimate(new UserId(userId));

        // Assert
        canEstimate.Should().Be(expectedCanEstimate);
    }

    [Fact]
    public void ParseAllowedUserIds_IgnoresBlankEntries()
    {
        // Act
        var allowedUserIds = ConfigurationMealEstimateAccessPolicy.ParseAllowedUserIds("firebase-uid-123,, ,firebase-uid-456,");

        // Assert
        allowedUserIds.Should().BeEquivalentTo("firebase-uid-123", "firebase-uid-456");
    }
}
