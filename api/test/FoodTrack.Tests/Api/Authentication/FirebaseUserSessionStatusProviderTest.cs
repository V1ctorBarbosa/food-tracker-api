using FluentAssertions;
using FoodTrack.Api.Authentication;

namespace FoodTrack.Tests.Api.Authentication;

public sealed class FirebaseUserSessionStatusProviderTest
{
    [Fact]
    public void ToSessionStatus_ReturnsInactive_WhenTheAccountIsDisabled()
    {
        // Act
        var status = FirebaseUserSessionStatusProvider.ToSessionStatus(true, DateTime.MinValue);

        // Assert
        status.Should().Be(UserSessionStatus.Inactive);
    }

    [Fact]
    public void ToSessionStatus_HasNoRevocation_WhenFirebaseReportsTheMinimumTimestamp()
    {
        // Act
        var status = FirebaseUserSessionStatusProvider.ToSessionStatus(false, DateTime.MinValue);

        // Assert
        status.Should().Be(new UserSessionStatus(true, null));
    }

    [Fact]
    public void ToSessionStatus_KeepsTheRevocationMomentAsUtc()
    {
        // Arrange
        var tokensValidAfterUtc = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Unspecified);

        // Act
        var status = FirebaseUserSessionStatusProvider.ToSessionStatus(false, tokensValidAfterUtc);

        // Assert
        status.IsAccountActive.Should().BeTrue();
        status.TokensValidAfter.Should().Be(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
    }
}
