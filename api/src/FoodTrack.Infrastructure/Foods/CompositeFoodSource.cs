using FoodTrack.Application.Foods;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// The <see cref="IExternalFoodSource"/> the app actually registers: queries USDA (generic
/// ingredients) and Open Food Facts (Brazilian branded products) in parallel and returns both
/// lists combined, so a search surfaces the raw ingredient and the specific packaged product
/// side by side. A source failing (e.g. a network error) degrades to just the other source's
/// results rather than failing the whole search.
/// </summary>
public sealed class CompositeFoodSource : IExternalFoodSource
{
    private readonly UsdaFoodDataCentralClient _usdaClient;
    private readonly OpenFoodFactsClient _openFoodFactsClient;

    public CompositeFoodSource(UsdaFoodDataCentralClient usdaClient, OpenFoodFactsClient openFoodFactsClient)
    {
        _usdaClient = usdaClient;
        _openFoodFactsClient = openFoodFactsClient;
    }

    public async Task<IReadOnlyList<Food>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var usdaResultsTask = SearchSafelyAsync(() => _usdaClient.SearchAsync(query, cancellationToken));
        var openFoodFactsResultsTask = SearchSafelyAsync(() => _openFoodFactsClient.SearchAsync(query, cancellationToken));

        var results = await Task.WhenAll(usdaResultsTask, openFoodFactsResultsTask);
        return results.SelectMany(foods => foods).ToList();
    }

    private static async Task<IReadOnlyList<Food>> SearchSafelyAsync(Func<Task<IReadOnlyList<Food>>> search)
    {
        try
        {
            return await search();
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}
