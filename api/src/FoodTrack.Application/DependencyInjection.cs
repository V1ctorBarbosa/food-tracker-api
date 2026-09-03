using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using Microsoft.Extensions.DependencyInjection;

namespace FoodTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<UserProfileService>();
        services.AddScoped<WeightEntryService>();
        services.AddScoped<WaterEntryService>();

        return services;
    }
}
