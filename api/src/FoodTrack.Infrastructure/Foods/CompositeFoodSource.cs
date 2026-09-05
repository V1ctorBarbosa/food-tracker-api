using FoodTrack.Application.Foods;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// The <see cref="IExternalFoodSource"/> the app actually registers: queries TACO (generic and
/// prepared Brazilian foods, each with an explicit raw/cooked preparation state) and Open Food
/// Facts (Brazilian branded products) in parallel and returns both lists combined, so a search
/// surfaces the generic food and the specific packaged product side by side. A source failing
/// (e.g. a network error) degrades to just the other source's results rather than failing the
/// whole search.
/// </summary>
public sealed class CompositeFoodSource : IExternalFoodSource
{
    private readonly TacoFoodDataClient _tacoClient;
    private readonly OpenFoodFactsClient _openFoodFactsClient;

    public CompositeFoodSource(TacoFoodDataClient tacoClient, OpenFoodFactsClient openFoodFactsClient)
    {
        _tacoClient = tacoClient;
        _openFoodFactsClient = openFoodFactsClient;
    }

    public async Task<IReadOnlyList<Food>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var tacoResultsTask = SearchSafelyAsync(() => _tacoClient.SearchAsync(query, cancellationToken));
        var openFoodFactsResultsTask = SearchSafelyAsync(() => _openFoodFactsClient.SearchAsync(query, cancellationToken));

        var results = await Task.WhenAll(tacoResultsTask, openFoodFactsResultsTask);
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
