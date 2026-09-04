using FoodTrack.Application.Dashboard;
using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
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
        services.AddScoped<FoodService>();
        services.AddScoped<MealService>();
        services.AddScoped<DashboardService>();

        return services;
    }
}
