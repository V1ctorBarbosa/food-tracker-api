using FoodTrack.Application.Foods;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Infrastructure.Firebase;
using FoodTrack.Infrastructure.Foods;
using FoodTrack.Infrastructure.Users;
using FoodTrack.Infrastructure.WaterEntries;
using FoodTrack.Infrastructure.WeightEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFirebaseAdmin(configuration);
        services.AddScoped<IUserRepository, FirestoreUserRepository>();
        services.AddScoped<IWeightEntryRepository, FirestoreWeightEntryRepository>();
        services.AddScoped<IWaterEntryRepository, FirestoreWaterEntryRepository>();
        services.AddScoped<IFoodRepository, FirestoreFoodRepository>();

        return services;
    }
}
