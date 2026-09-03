using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure.Firebase;

public static class FirebaseServiceCollectionExtensions
{
    public static IServiceCollection AddFirebaseAdmin(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FirebaseOptions>()
            .Bind(configuration.GetSection(FirebaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;
            return CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialsPath).ToGoogleCredential();
        });

        services.AddSingleton(sp =>
        {
            var credential = sp.GetRequiredService<GoogleCredential>();
            var options = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;

            return FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = options.ProjectId,
            });
        });

        services.AddSingleton(sp =>
        {
            var credential = sp.GetRequiredService<GoogleCredential>();
            var options = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;

            return new FirestoreDbBuilder
            {
                ProjectId = options.ProjectId,
                GoogleCredential = credential,
            }.Build();
        });

        return services;
    }
}
