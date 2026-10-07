using System.ComponentModel.DataAnnotations;

namespace FoodTrack.Infrastructure.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    [Required(AllowEmptyStrings = false)]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Path to the service account key file, used for local dev. Exactly one of this or
    /// <see cref="CredentialsJson"/> must be set -- enforced in AddFirebaseAdmin, since
    /// DataAnnotations can't express "one of these two" on its own.
    /// </summary>
    public string CredentialsPath { get; set; } = string.Empty;

    /// <summary>
    /// The service account key's full JSON content, used instead of <see cref="CredentialsPath"/>
    /// when set (e.g. from a Lambda environment variable, where there's no credentials file on
    /// disk to point at). Empty by default so local dev keeps reading the file as it always has.
    /// </summary>
    public string CredentialsJson { get; set; } = string.Empty;
}
