using FoodTrack.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace FoodTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<UserProfileService>();

        return services;
    }
}
