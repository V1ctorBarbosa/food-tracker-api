using System.ComponentModel.DataAnnotations;

namespace FoodTrack.Infrastructure.MealPhotos;

public sealed class MealPhotoStorageOptions
{
    public const string SectionName = "MealPhotos";

    /// <summary>Set on the deployed function from Terraform, which owns the bucket.</summary>
    [Required(AllowEmptyStrings = false)]
    public string BucketName { get; set; } = string.Empty;
}
