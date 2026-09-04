using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Infrastructure.Firebase;
using FoodTrack.Infrastructure.Foods;
using FoodTrack.Infrastructure.Meals;
using FoodTrack.Infrastructure.Users;
using FoodTrack.Infrastructure.WaterEntries;
using FoodTrack.Infrastructure.WeightEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodTrack.Infrastructure;

public static class DependencyInjection
{
    private const string UsdaFoodDataCentralBaseUrl = "https://api.nal.usda.gov/fdc/v1/";
    private const string OpenFoodFactsBaseUrl = "https://search.openfoodfacts.org/";

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFirebaseAdmin(configuration);
        services.AddScoped<IUserRepository, FirestoreUserRepository>();
        services.AddScoped<IWeightEntryRepository, FirestoreWeightEntryRepository>();
        services.AddScoped<IWaterEntryRepository, FirestoreWaterEntryRepository>();
        services.AddScoped<IFoodRepository, FirestoreFoodRepository>();
        services.AddScoped<IMealRepository, FirestoreMealRepository>();

        services
            .AddOptions<UsdaFoodDataCentralOptions>()
            .Bind(configuration.GetSection(UsdaFoodDataCentralOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<UsdaFoodDataCentralClient>(client =>
        {
            client.BaseAddress = new Uri(UsdaFoodDataCentralBaseUrl);
        });

        services.AddHttpClient<OpenFoodFactsClient>(client =>
        {
            client.BaseAddress = new Uri(OpenFoodFactsBaseUrl);
        });

        services.AddScoped<IExternalFoodSource, CompositeFoodSource>();

        return services;
    }
}
