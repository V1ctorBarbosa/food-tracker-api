using FluentAssertions;
using FoodTrack.Api.Authentication;
using FoodTrack.Api.ErrorHandling;

namespace FoodTrack.Tests.Api.Authentication;

public sealed class FirebaseAuthErrorCodeMapperTest
{
    [Fact]
    public void Map_ReturnsTokenExpired_WhenFirebaseErrorCodeIsExpiredIdToken()
    {
        // Act
        var errorCode = FirebaseAuthErrorCodeMapper.Map(FirebaseAdmin.Auth.AuthErrorCode.ExpiredIdToken);

        // Assert
        errorCode.Should().Be(AuthErrorCode.TokenExpired);
    }

    [Fact]
    public void Map_ReturnsTokenRevoked_WhenFirebaseErrorCodeIsRevokedIdToken()
    {
        // Act
        var errorCode = FirebaseAuthErrorCodeMapper.Map(FirebaseAdmin.Auth.AuthErrorCode.RevokedIdToken);

        // Assert
        errorCode.Should().Be(AuthErrorCode.TokenRevoked);
    }

    [Theory]
    [InlineData(FirebaseAdmin.Auth.AuthErrorCode.InvalidIdToken)]
    [InlineData(FirebaseAdmin.Auth.AuthErrorCode.CertificateFetchFailed)]
    [InlineData(FirebaseAdmin.Auth.AuthErrorCode.UserNotFound)]
    public void Map_ReturnsTokenInvalid_ForAnyOtherFirebaseErrorCode(FirebaseAdmin.Auth.AuthErrorCode firebaseAuthErrorCode)
    {
        // Act
        var errorCode = FirebaseAuthErrorCodeMapper.Map(firebaseAuthErrorCode);

        // Assert
        errorCode.Should().Be(AuthErrorCode.TokenInvalid);
    }

    [Fact]
    public void Map_ReturnsTokenInvalid_WhenFirebaseErrorCodeIsNull()
    {
        // Act
        var errorCode = FirebaseAuthErrorCodeMapper.Map(null);

        // Assert
        errorCode.Should().Be(AuthErrorCode.TokenInvalid);
    }
}
