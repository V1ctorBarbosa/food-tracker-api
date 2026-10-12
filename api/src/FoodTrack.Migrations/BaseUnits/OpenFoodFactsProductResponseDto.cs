using System.Text.Json.Serialization;

namespace FoodTrack.Migrations.BaseUnits;

/// <summary>Raw wire shape of Open Food Facts' GET /api/v2/product/{barcode}, limited to the fields requested.</summary>
internal sealed class OpenFoodFactsProductResponseDto
{
    /// <summary>1 when the product was found, 0 otherwise.</summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("product")]
    public OpenFoodFactsProductDto? Product { get; set; }
}

internal sealed class OpenFoodFactsProductDto
{
    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("categories_tags")]
    public List<string> CategoriesTags { get; set; } = [];
}
