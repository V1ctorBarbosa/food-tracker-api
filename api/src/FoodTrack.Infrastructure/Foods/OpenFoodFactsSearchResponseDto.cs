using System.Text.Json.Serialization;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Raw wire shape of an Open Food Facts search-a-licious GET /search response. Deliberately kept
/// separate from the domain model -- only <see cref="OpenFoodFactsClient"/> ever sees this shape.
/// Verified against a live response rather than the published schema.
/// </summary>
internal sealed class OpenFoodFactsSearchResponseDto
{
    [JsonPropertyName("hits")]
    public List<OpenFoodFactsHitDto> Hits { get; set; } = [];
}

internal sealed class OpenFoodFactsHitDto
{
    /// <summary>The product's barcode (EAN/GTIN) -- Open Food Facts' natural product identifier.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("brands")]
    public List<string> Brands { get; set; } = [];

    /// <summary>Free-text package size, e.g. "350ml", "1 L" or "500 g".</summary>
    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("categories_tags")]
    public List<string> CategoriesTags { get; set; } = [];

    [JsonPropertyName("nutriments")]
    public OpenFoodFactsNutrimentsDto? Nutriments { get; set; }
}

internal sealed class OpenFoodFactsNutrimentsDto
{
    [JsonPropertyName("energy-kcal_100g")]
    public double? EnergyKcalPer100g { get; set; }

    [JsonPropertyName("proteins_100g")]
    public double? ProteinsPer100g { get; set; }

    [JsonPropertyName("carbohydrates_100g")]
    public double? CarbohydratesPer100g { get; set; }

    [JsonPropertyName("fat_100g")]
    public double? FatPer100g { get; set; }
}
