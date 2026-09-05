using System.Text.Json.Serialization;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>Top-level shape of TACO's foods.json: {"total": 597, "foods": [...]}.</summary>
public sealed class TacoFoodListResponseDto
{
    [JsonPropertyName("foods")]
    public List<TacoFoodSummaryDto> Foods { get; set; } = [];
}

/// <summary>Entry from TACO's foods.json listing -- just enough to filter by name before
/// fetching the full nutrient record for a match.</summary>
public sealed class TacoFoodSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>Full record from TACO's foods/{id}.json -- only the 4 core macros are mapped;
/// TACO reports many more (fiber, vitamins, fatty acids) that Food doesn't model today.</summary>
public sealed class TacoFoodDetailDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("energy_kcal")]
    public double? EnergyKcal { get; set; }

    [JsonPropertyName("protein_g")]
    public double? ProteinG { get; set; }

    [JsonPropertyName("lipids_g")]
    public double? LipidsG { get; set; }

    [JsonPropertyName("carbohydrate_g")]
    public double? CarbohydrateG { get; set; }
}
