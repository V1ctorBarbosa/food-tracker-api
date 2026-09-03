using System.Security.Claims;
using FluentAssertions;
using FoodTrack.Api.Authentication;

namespace FoodTrack.Tests.Api.Authentication;

public sealed class ClaimsPrincipalExtensionsTest
{
    [Fact]
    public void GetAuthenticatedUserId_ReturnsUserId_FromNameIdentifierClaim()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "firebase-uid-123")]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var userId = principal.GetAuthenticatedUserId();

        // Assert
        userId.Value.Should().Be("firebase-uid-123");
    }

    [Fact]
    public void GetAuthenticatedUserId_Throws_WhenNameIdentifierClaimIsMissing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var act = () => principal.GetAuthenticatedUserId();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
