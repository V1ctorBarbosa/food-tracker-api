using FluentAssertions;
using FoodTrack.Api.Authentication;

namespace FoodTrack.Tests.Api.Authentication;

public sealed class UserSessionStatusTest
{
    private static readonly DateTimeOffset RevokedAt = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rejects_ReturnsFalse_WhenTheAccountIsActiveAndWasNeverRevoked()
    {
        // Arrange
        var status = new UserSessionStatus(true, null);

        // Act
        var rejects = status.Rejects(RevokedAt);

        // Assert
        rejects.Should().BeFalse();
    }

    [Fact]
    public void Rejects_ReturnsTrue_ForATokenIssuedBeforeTheRevocation()
    {
        // Arrange
        var status = new UserSessionStatus(true, RevokedAt);

        // Act
        var rejects = status.Rejects(RevokedAt.AddSeconds(-1));

        // Assert
        rejects.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void Rejects_ReturnsFalse_ForATokenIssuedAtOrAfterTheRevocation(int secondsAfterRevocation)
    {
        // Arrange
        var status = new UserSessionStatus(true, RevokedAt);

        // Act
        var rejects = status.Rejects(RevokedAt.AddSeconds(secondsAfterRevocation));

        // Assert
        rejects.Should().BeFalse();
    }

    [Fact]
    public void Rejects_ReturnsTrue_ForAnyToken_WhenTheAccountIsInactive()
    {
        // Act
        var rejects = UserSessionStatus.Inactive.Rejects(DateTimeOffset.UtcNow);

        // Assert
        rejects.Should().BeTrue();
    }
}
