using Microsoft.Extensions.Configuration;

namespace FoodTrack.Infrastructure.Configuration;

/// <summary>
/// Loads every parameter under a Systems Manager Parameter Store path into configuration, so
/// secrets like the Firebase service account key are read at startup from an encrypted
/// parameter instead of sitting in plain text in the function's environment. A parameter named
/// <c>{path}Firebase/CredentialsJson</c> becomes the <c>Firebase:CredentialsJson</c> setting.
/// </summary>
public static class ParameterStoreConfigurationExtensions
{
    public const string PathKey = "AwsParameterStore:Path";

    /// <summary>
    /// Adds the Parameter Store source only when <see cref="PathKey"/> is set, which is the case on
    /// the deployed function and not in local development. A configured path that cannot be read
    /// fails startup rather than letting the API boot without its credentials.
    /// </summary>
    public static IConfigurationManager AddParameterStoreWhenConfigured(this IConfigurationManager configuration)
    {
        var path = ReadPath(configuration);
        if (path is null)
        {
            return configuration;
        }

        configuration.AddSystemsManager(path);
        return configuration;
    }

    /// <returns>The configured Parameter Store path, or null when none is set.</returns>
    public static string? ReadPath(IConfiguration configuration)
    {
        var path = configuration[PathKey];
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path;
    }
}
