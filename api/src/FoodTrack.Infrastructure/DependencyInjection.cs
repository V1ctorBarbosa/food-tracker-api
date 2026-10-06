using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Infrastructure.Firebase;
using FoodTrack.Infrastructure.Foods;
using FoodTrack.Infrastructure.Meals;
using FoodTrack.Infrastructure.Startup;
using FoodTrack.Infrastructure.Users;
using FoodTrack.Infrastructure.WaterEntries;
using FoodTrack.Infrastructure.WeightEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodTrack.Infrastructure;

public static class DependencyInjection
{
    private const string TacoBaseUrl = "https://brolesi.github.io/taco/";
    private const string OpenFoodFactsBaseUrl = "https://search.openfoodfacts.org/";

    // Well under the function timeout, so a slow source is dropped from the search instead of
    // holding the whole request (and the billed function time) until the platform kills it.
    private static readonly TimeSpan ExternalFoodSourceTimeout = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFirebaseAdmin(configuration);
        services.AddSingleton<IStartupWarmUp, FirestoreWarmUp>();
        services.AddHostedService<StartupWarmUpHostedService>();
        services.AddScoped<IUserRepository, FirestoreUserRepository>();
        services.AddScoped<IWeightEntryRepository, FirestoreWeightEntryRepository>();
        services.AddScoped<IWaterEntryRepository, FirestoreWaterEntryRepository>();
        services.AddScoped<IFoodRepository, FirestoreFoodRepository>();
        services.AddScoped<IMealRepository, FirestoreMealRepository>();
        services.AddSingleton<IIdentityAccountRemover, FirebaseIdentityAccountRemover>();

        services.AddHttpClient<TacoFoodDataClient>(client =>
        {
            client.BaseAddress = new Uri(TacoBaseUrl);
            client.Timeout = ExternalFoodSourceTimeout;
        });

        services.AddHttpClient<OpenFoodFactsClient>(client =>
        {
            client.BaseAddress = new Uri(OpenFoodFactsBaseUrl);
            client.Timeout = ExternalFoodSourceTimeout;
        });

        services.AddScoped<IExternalFoodSource, CompositeFoodSource>();

        return services;
    }
}
