using System.Text;
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
            return LoadCredential(options);
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

    /// <summary>
    /// CredentialsJson wins when set (loaded from Parameter Store on the deployed function, where
    /// there is no file on disk to point at); otherwise falls back to reading CredentialsPath, as
    /// local dev always has.
    /// </summary>
    private static GoogleCredential LoadCredential(FirebaseOptions options)
    {
        if (!string.IsNullOrEmpty(options.CredentialsJson))
        {
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(options.CredentialsJson));
            return CredentialFactory.FromStream<ServiceAccountCredential>(jsonStream).ToGoogleCredential();
        }

        if (!string.IsNullOrEmpty(options.CredentialsPath))
        {
            return CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialsPath).ToGoogleCredential();
        }

        throw new InvalidOperationException(
            $"Firebase credentials are not configured -- set either {nameof(FirebaseOptions.CredentialsPath)} or {nameof(FirebaseOptions.CredentialsJson)}.");
    }
}
