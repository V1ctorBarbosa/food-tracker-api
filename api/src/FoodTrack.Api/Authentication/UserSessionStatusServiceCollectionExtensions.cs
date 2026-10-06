using Microsoft.Extensions.Caching.Memory;

namespace FoodTrack.Api.Authentication;

public static class UserSessionStatusServiceCollectionExtensions
{
    /// <summary>Registers the cached Firebase-backed <see cref="IUserSessionStatusProvider"/>.</summary>
    public static IServiceCollection AddUserSessionStatus(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<FirebaseUserSessionStatusProvider>();
        services.AddSingleton<IUserSessionStatusProvider>(serviceProvider => new CachedUserSessionStatusProvider(
            serviceProvider.GetRequiredService<FirebaseUserSessionStatusProvider>(),
            serviceProvider.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
