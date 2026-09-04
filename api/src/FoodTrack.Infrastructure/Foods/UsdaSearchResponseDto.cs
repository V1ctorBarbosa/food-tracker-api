using System.Text.Json.Serialization;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Raw wire shape of a USDA FoodData Central GET /foods/search response. Deliberately kept
/// separate from the domain model -- only <see cref="UsdaFoodDataCentralClient"/> ever sees
/// this shape. Verified against a live response rather than the published OpenAPI spec,
/// which documents different field names (number/amount) than the API actually returns.
/// </summary>
internal sealed class UsdaSearchResponseDto
{
    [JsonPropertyName("foods")]
    public List<UsdaFoodSearchResultDto> Foods { get; set; } = [];
}

internal sealed class UsdaFoodSearchResultDto
{
    [JsonPropertyName("fdcId")]
    public int FdcId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("foodNutrients")]
    public List<UsdaAbridgedNutrientDto> FoodNutrients { get; set; } = [];
}

internal sealed class UsdaAbridgedNutrientDto
{
    /// <summary>USDA's standardized nutrient number (e.g. "208" for Energy in kcal) -- stable across data types, unlike the display name.</summary>
    [JsonPropertyName("nutrientNumber")]
    public string NutrientNumber { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unitName")]
    public string UnitName { get; set; } = string.Empty;
}
