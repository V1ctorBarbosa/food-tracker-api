using System.Globalization;
using System.Net.Http.Json;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Queries the USDA FoodData Central search endpoint (docs section 14). Reports values per
/// 100g uniformly: USDA normalizes nutrient amounts to 100g for every data type it returns,
/// including branded/liquid foods, so a Milliliter base unit is never needed for USDA data.
/// </summary>
public sealed class UsdaFoodDataCentralClient : IExternalFoodSource
{
    // USDA's standardized nutrient numbers (stable across data types) -- see docs section 14.
    private const string EnergyKcalNutrientNumber = "208";
    private const string ProteinNutrientNumber = "203";
    private const string FatNutrientNumber = "204";
    private const string CarbohydrateNutrientNumber = "205";

    private readonly HttpClient _httpClient;
    private readonly UsdaFoodDataCentralOptions _options;

    public UsdaFoodDataCentralClient(HttpClient httpClient, IOptions<UsdaFoodDataCentralOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<Food>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var requestUri = $"foods/search?api_key={Uri.EscapeDataString(_options.ApiKey)}&query={Uri.EscapeDataString(query)}&pageSize=10";
        var response = await _httpClient.GetFromJsonAsync<UsdaSearchResponseDto>(requestUri, cancellationToken);

        if (response is null)
        {
            return [];
        }

        return response.Foods
            .Select(ToFood)
            .Where(food => food is not null)
            .Select(food => food!)
            .ToList();
    }

    /// <summary>Returns null when the result is missing one of the four core macros -- not usable as a Food yet.</summary>
    private static Food? ToFood(UsdaFoodSearchResultDto result)
    {
        var calories = FindNutrientAmount(result.FoodNutrients, EnergyKcalNutrientNumber);
        var protein = FindNutrientAmount(result.FoodNutrients, ProteinNutrientNumber);
        var fat = FindNutrientAmount(result.FoodNutrients, FatNutrientNumber);
        var carbs = FindNutrientAmount(result.FoodNutrients, CarbohydrateNutrientNumber);

        if (calories is null || protein is null || fat is null || carbs is null)
        {
            return null;
        }

        return Food.Create(
            FoodId.NewId(),
            result.Description,
            FoodSource.Usda,
            result.FdcId.ToString(CultureInfo.InvariantCulture),
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories((int)Math.Round(calories.Value)),
            new Grams(protein.Value),
            new Grams(carbs.Value),
            new Grams(fat.Value));
    }

    private static double? FindNutrientAmount(List<UsdaAbridgedNutrientDto> nutrients, string nutrientNumber) =>
        nutrients.FirstOrDefault(nutrient => nutrient.NutrientNumber == nutrientNumber)?.Value;
}
