using FluentAssertions;
using FoodTrack.Api.Authentication;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace FoodTrack.Tests.Api.Authentication;

public sealed class CachedUserSessionStatusProviderTest : IDisposable
{
    private readonly IUserSessionStatusProvider _innerProvider = Substitute.For<IUserSessionStatusProvider>();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly CachedUserSessionStatusProvider _sut;

    public CachedUserSessionStatusProviderTest()
    {
        _sut = new CachedUserSessionStatusProvider(_innerProvider, _memoryCache);
    }

    [Fact]
    public async Task GetAsync_AsksTheInnerProviderOnce_ForRepeatedLookupsOfTheSameUser()
    {
        // Arrange
        var activeStatus = new UserSessionStatus(true, null);
        _innerProvider.GetAsync("firebase-uid-123", Arg.Any<CancellationToken>()).Returns(activeStatus);

        // Act
        var firstStatus = await _sut.GetAsync("firebase-uid-123");
        var secondStatus = await _sut.GetAsync("firebase-uid-123");

        // Assert
        firstStatus.Should().Be(activeStatus);
        secondStatus.Should().Be(activeStatus);
        await _innerProvider.Received(1).GetAsync("firebase-uid-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_KeepsEachUsersStatusSeparate()
    {
        // Arrange
        var activeStatus = new UserSessionStatus(true, null);
        _innerProvider.GetAsync("firebase-uid-123", Arg.Any<CancellationToken>()).Returns(activeStatus);
        _innerProvider.GetAsync("firebase-uid-456", Arg.Any<CancellationToken>()).Returns(UserSessionStatus.Inactive);

        // Act
        var firstUserStatus = await _sut.GetAsync("firebase-uid-123");
        var secondUserStatus = await _sut.GetAsync("firebase-uid-456");

        // Assert
        firstUserStatus.Should().Be(activeStatus);
        secondUserStatus.Should().Be(UserSessionStatus.Inactive);
    }

    [Fact]
    public void CacheDuration_IsFiveMinutes()
    {
        CachedUserSessionStatusProvider.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}
