using System.ComponentModel.DataAnnotations;

namespace FoodTrack.Infrastructure.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    [Required(AllowEmptyStrings = false)]
    public string ProjectId { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string CredentialsPath { get; set; } = string.Empty;
}
