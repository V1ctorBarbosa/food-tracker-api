using System.ComponentModel.DataAnnotations;

namespace FoodTrack.Infrastructure.Foods;

public sealed class UsdaFoodDataCentralOptions
{
    public const string SectionName = "UsdaFoodDataCentral";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;
}
