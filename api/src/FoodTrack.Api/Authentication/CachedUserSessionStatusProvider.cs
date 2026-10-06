using Microsoft.Extensions.Caching.Memory;

namespace FoodTrack.Api.Authentication;

/// <summary>
/// Remembers each user's session status for a short while, so revocation is checked without a call
/// to the identity provider on every request. The trade-off is that a revoked or disabled account
/// keeps working for at most <see cref="CacheDuration"/>, instead of up to the token's full
/// one-hour lifetime when revocation is never checked.
/// </summary>
public sealed class CachedUserSessionStatusProvider : IUserSessionStatusProvider
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private const string CacheKeyPrefix = "user-session-status:";

    private readonly IUserSessionStatusProvider _innerProvider;
    private readonly IMemoryCache _memoryCache;

    public CachedUserSessionStatusProvider(IUserSessionStatusProvider innerProvider, IMemoryCache memoryCache)
    {
        _innerProvider = innerProvider;
        _memoryCache = memoryCache;
    }

    public async Task<UserSessionStatus> GetAsync(string uid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + uid;
        if (_memoryCache.TryGetValue(cacheKey, out UserSessionStatus? cachedStatus) && cachedStatus is not null)
        {
            return cachedStatus;
        }

        var status = await _innerProvider.GetAsync(uid, cancellationToken);
        _memoryCache.Set(cacheKey, status, CacheDuration);
        return status;
    }
}
