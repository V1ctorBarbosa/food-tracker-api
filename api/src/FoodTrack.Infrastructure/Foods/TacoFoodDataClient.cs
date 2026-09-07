using System.Globalization;
using System.Net.Http.Json;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// Queries the community-maintained static-JSON mirror of TACO (NEPA/UNICAMP) -- Brazilian,
/// Portuguese, and unlike USDA/Open Food Facts, each entry already states its preparation state
/// (cru, cozido, etc) explicitly, rather than defaulting to "as sold" for staples like rice.
/// There is no search endpoint: foods.json lists every food (id + description only), so a
/// search filters that list by name and fetches full records only for the matches.
/// </summary>
public sealed class TacoFoodDataClient : IExternalFoodSource
{
    private const int MaxResultCount = 10;

    private readonly HttpClient _httpClient;

    public TacoFoodDataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Food>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<TacoFoodListResponseDto>("foods.json", cancellationToken);
        if (response is null)
        {
            return [];
        }

        var matchingIds = response.Foods
            .Where(summary => FoodNameMatcher.MatchesAllWords(summary.Description, query))
            .Take(MaxResultCount)
            .Select(summary => summary.Id);

        var detailFetches = matchingIds.Select(id => _httpClient.GetFromJsonAsync<TacoFoodDetailDto>($"foods/{id}.json", cancellationToken));
        var details = await Task.WhenAll(detailFetches);

        return details.Select(ToFood).OfType<Food>().ToList();
    }

    /// <summary>Returns null when the result has no name or is missing one of the four core macros.</summary>
    private static Food? ToFood(TacoFoodDetailDto? detail)
    {
        if (detail is null || string.IsNullOrWhiteSpace(detail.Description))
        {
            return null;
        }

        if (detail.EnergyKcal is not { } calories ||
            detail.ProteinG is not { } protein ||
            detail.CarbohydrateG is not { } carbs ||
            detail.LipidsG is not { } fat)
        {
            return null;
        }

        return Food.Create(
            FoodId.NewId(),
            detail.Description,
            FoodSource.Taco,
            detail.Id.ToString(CultureInfo.InvariantCulture),
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories((int)Math.Round(calories)),
            new Grams(protein),
            new Grams(carbs),
            new Grams(fat));
    }
}
