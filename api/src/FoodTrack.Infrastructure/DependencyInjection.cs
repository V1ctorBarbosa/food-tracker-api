using FoodTrack.Application.Users;
using FoodTrack.Infrastructure.Firebase;
using FoodTrack.Infrastructure.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFirebaseAdmin(configuration);
        services.AddScoped<IUserRepository, FirestoreUserRepository>();

        return services;
    }
}
