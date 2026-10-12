using System.Net;
using System.Net.Http.Json;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Migrations.BaseUnits;

/// <summary>
/// Classifies cached Open Food Facts foods by looking each product up by barcode, since the cache
/// kept neither the package quantity nor the categories the decision needs.
/// </summary>
internal sealed class OpenFoodFactsFoodUnitClassifier : IFoodUnitClassifier
{
    private const string ProductFields = "quantity,categories_tags";
    private const int ProductFoundStatus = 1;

    private readonly HttpClient _httpClient;
    private readonly OpenFoodFactsRequestPacing _pacing;

    public OpenFoodFactsFoodUnitClassifier(HttpClient httpClient, OpenFoodFactsRequestPacing pacing)
    {
        _httpClient = httpClient;
        _pacing = pacing;
    }

    public FoodSource Source => FoodSource.OpenFoodFacts;

    public async Task<MeasurementUnit?> ClassifyAsync(Food food, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(food.ExternalId))
        {
            return null;
        }

        var product = await FetchProductAsync(food.ExternalId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        return OpenFoodFactsBaseUnitResolver.Resolve(product.Quantity, product.CategoriesTags);
    }

    // A 429 is retried after the wait Open Food Facts asks for. Anything else that fails (a product
    // deleted since it was cached comes back as 404, a network error) leaves that one food
    // unclassified rather than stopping the whole run.
    private async Task<OpenFoodFactsProductDto?> FetchProductAsync(string barcode, CancellationToken cancellationToken)
    {
        var requestUri = $"api/v2/product/{Uri.EscapeDataString(barcode)}?fields={ProductFields}";
        for (var attempt = 1; attempt <= _pacing.MaximumAttempts; attempt++)
        {
            await Task.Delay(_pacing.IntervalBetweenRequests, cancellationToken);

            using var response = await SendAsync(requestUri, cancellationToken);
            if (response is null)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(ResolveWaitAfterRateLimit(response), cancellationToken);
                continue;
            }

            return await ReadProductAsync(response, cancellationToken);
        }

        return null;
    }

    private async Task<HttpResponseMessage?> SendAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private TimeSpan ResolveWaitAfterRateLimit(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } requestedWait)
        {
            return requestedWait;
        }

        return _pacing.WaitAfterRateLimit;
    }

    private static async Task<OpenFoodFactsProductDto?> ReadProductAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<OpenFoodFactsProductResponseDto>(cancellationToken);
        if (body is null || body.Status != ProductFoundStatus)
        {
            return null;
        }

        return body.Product;
    }
}
