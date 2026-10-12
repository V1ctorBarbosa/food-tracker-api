using System.ComponentModel.DataAnnotations;

namespace FoodTrack.Infrastructure.MealEstimates;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    /// <summary>Read from the encrypted Parameter Store parameter <c>{path}Anthropic/ApiKey</c> on the deployed function.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = "claude-haiku-5-5";
}
