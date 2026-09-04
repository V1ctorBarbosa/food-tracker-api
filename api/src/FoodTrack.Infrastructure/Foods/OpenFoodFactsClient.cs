using System.Net.Http.Json;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Queries Open Food Facts' search-a-licious endpoint, scoped to Brazilian products in
/// Portuguese -- the crowdsourced branded-product counterpart to <see cref="UsdaFoodDataCentralClient"/>'s
/// generic ingredients (docs section 14). Like USDA, every result is reported per 100g: Open Food
/// Facts' nutriment fields use the "_100g" suffix uniformly, including for liquids.
/// </summary>
public sealed class OpenFoodFactsClient : IExternalFoodSource
{
    // Scopes results to Brazilian-market products in Portuguese -- without this, search-a-licious
    // defaults to matching English product names only, which excludes almost every Brazilian entry.
    private const string BrazilCountryFilter = "countries_tags:\"en:brazil\"";
    private const string PortugueseLanguage = "pt";
    private const int PageSize = 10;

    private readonly HttpClient _httpClient;

    public OpenFoodFactsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Food>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchExpression = $"{query} {BrazilCountryFilter}";
        var requestUri =
            $"search?q={Uri.EscapeDataString(searchExpression)}&langs={PortugueseLanguage}&page_size={PageSize}" +
            "&fields=code,product_name,brands,nutriments";

        var response = await _httpClient.GetFromJsonAsync<OpenFoodFactsSearchResponseDto>(requestUri, cancellationToken);
        if (response is null)
        {
            return [];
        }

        return response.Hits
            .Select(ToFood)
            .Where(food => food is not null)
            .Select(food => food!)
            .ToList();
    }

    /// <summary>Returns null when the result has no name or is missing one of the four core macros.</summary>
    private static Food? ToFood(OpenFoodFactsHitDto hit)
    {
        if (string.IsNullOrWhiteSpace(hit.ProductName))
        {
            return null;
        }

        var nutriments = hit.Nutriments;
        if (nutriments is null ||
            nutriments.EnergyKcalPer100g is not { } calories ||
            nutriments.ProteinsPer100g is not { } protein ||
            nutriments.CarbohydratesPer100g is not { } carbs ||
            nutriments.FatPer100g is not { } fat)
        {
            return null;
        }

        return Food.Create(
            FoodId.NewId(),
            BuildDisplayName(hit),
            FoodSource.OpenFoodFacts,
            hit.Code,
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories((int)Math.Round(calories)),
            new Grams(protein),
            new Grams(carbs),
            new Grams(fat));
    }

    /// <summary>Appends the brand so visually-identical generic names (e.g. several "White bread"
    /// entries) are actually distinguishable in a search result list.</summary>
    private static string BuildDisplayName(OpenFoodFactsHitDto hit)
    {
        if (hit.Brands.Count == 0)
        {
            return hit.ProductName!;
        }

        return $"{hit.ProductName} ({string.Join(", ", hit.Brands)})";
    }
}
