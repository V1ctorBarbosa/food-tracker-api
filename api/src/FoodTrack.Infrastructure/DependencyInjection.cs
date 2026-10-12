using Amazon.S3;
using Anthropic;
using FoodTrack.Application.Foods;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Application.Meals;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Infrastructure.Firebase;
using FoodTrack.Infrastructure.Foods;
using FoodTrack.Infrastructure.MealEstimates;
using FoodTrack.Infrastructure.MealPhotos;
using FoodTrack.Infrastructure.Meals;
using FoodTrack.Infrastructure.Startup;
using FoodTrack.Infrastructure.Users;
using FoodTrack.Infrastructure.WaterEntries;
using FoodTrack.Infrastructure.WeightEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure;

public static class DependencyInjection
{
    private const string TacoBaseUrl = "https://brolesi.github.io/taco/";
    private const string OpenFoodFactsBaseUrl = "https://search.openfoodfacts.org/";

    // Well under the function timeout, so a slow source is dropped from the search instead of
    // holding the whole request (and the billed function time) until the platform kills it.
    private static readonly TimeSpan ExternalFoodSourceTimeout = TimeSpan.FromSeconds(10);

    // Two attempts of this length still end inside the function's 60 s timeout.
    private static readonly TimeSpan EstimatorTimeout = TimeSpan.FromSeconds(25);
    private const int EstimatorMaximumRetries = 1;

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

        services.AddMealEstimates(configuration);

        return services;
    }

    /// <summary>
    /// The options here are validated when first used, not at startup, so local development runs
    /// without an Anthropic key or a bucket until an estimate or a photo is actually requested.
    /// </summary>
    private static void AddMealEstimates(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.SectionName)).ValidateDataAnnotations();
        services.AddOptions<MealPhotoStorageOptions>().Bind(configuration.GetSection(MealPhotoStorageOptions.SectionName)).ValidateDataAnnotations();
        services.AddOptions<MealEstimateAccessOptions>().Bind(configuration.GetSection(MealEstimateAccessOptions.SectionName)).ValidateDataAnnotations();

        services.AddSingleton(serviceProvider => new AnthropicClient
        {
            ApiKey = serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey,
            Timeout = EstimatorTimeout,
            MaxRetries = EstimatorMaximumRetries,
        });
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());

        services.AddScoped<IMealEstimator, ClaudeMealEstimator>();
        services.AddScoped<IMealPhotoStorage, S3MealPhotoStorage>();
        services.AddSingleton<IMealEstimateAccessPolicy, ConfigurationMealEstimateAccessPolicy>();
        services.AddScoped<IMealEstimateQuota, FirestoreMealEstimateQuota>();
    }
}
