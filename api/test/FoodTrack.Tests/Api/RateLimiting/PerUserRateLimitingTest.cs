using System.Security.Claims;
using FluentAssertions;
using FoodTrack.Api.RateLimiting;

namespace FoodTrack.Tests.Api.RateLimiting;

public sealed class PerUserRateLimitingTest
{
    [Fact]
    public void PartitionKeyFor_ReturnsTheUserId_FromTheNameIdentifierClaim()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "firebase-uid-123")], "Firebase");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var partitionKey = PerUserRateLimiting.PartitionKeyFor(principal);

        // Assert
        partitionKey.Should().Be("firebase-uid-123");
    }

    [Fact]
    public void PartitionKeyFor_ReturnsNull_WhenNobodyIsSignedIn()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var partitionKey = PerUserRateLimiting.PartitionKeyFor(principal);

        // Assert
        partitionKey.Should().BeNull();
    }
}
