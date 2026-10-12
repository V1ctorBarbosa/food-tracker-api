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

    // Open Food Facts allows 100 product reads per minute per client.
    private static readonly TimeSpan IntervalBetweenRequests = TimeSpan.FromMilliseconds(700);

    private readonly HttpClient _httpClient;

    public OpenFoodFactsFoodUnitClassifier(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public FoodSource Source => FoodSource.OpenFoodFacts;

    public async Task<MeasurementUnit?> ClassifyAsync(Food food, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(food.ExternalId))
        {
            return null;
        }

        await Task.Delay(IntervalBetweenRequests, cancellationToken);

        var product = await FetchProductAsync(food.ExternalId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        return OpenFoodFactsBaseUnitResolver.Resolve(product.Quantity, product.CategoriesTags);
    }

    // A product deleted since it was cached comes back as 404, and a failed request should leave
    // that one food unclassified rather than stop the whole run.
    private async Task<OpenFoodFactsProductDto?> FetchProductAsync(string barcode, CancellationToken cancellationToken)
    {
        var requestUri = $"api/v2/product/{Uri.EscapeDataString(barcode)}?fields={ProductFields}";
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
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
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
