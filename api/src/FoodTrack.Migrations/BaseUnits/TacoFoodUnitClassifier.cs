using System.Globalization;
using System.Net.Http.Json;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Migrations.BaseUnits;

/// <summary>Classifies cached TACO foods by the category TACO's food list gives each id, fetched once.</summary>
internal sealed class TacoFoodUnitClassifier : IFoodUnitClassifier
{
    private const string FoodListPath = "foods.json";

    private readonly HttpClient _httpClient;
    private IReadOnlyDictionary<int, string?>? _categoriesById;

    public TacoFoodUnitClassifier(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public FoodSource Source => FoodSource.Taco;

    public async Task<MeasurementUnit?> ClassifyAsync(Food food, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(food.ExternalId, NumberStyles.None, CultureInfo.InvariantCulture, out var tacoId))
        {
            return null;
        }

        var categoriesById = await LoadCategoriesAsync(cancellationToken);
        if (!categoriesById.TryGetValue(tacoId, out var category))
        {
            return null;
        }

        return TacoBaseUnitResolver.Resolve(tacoId, category);
    }

    private async Task<IReadOnlyDictionary<int, string?>> LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        if (_categoriesById is not null)
        {
            return _categoriesById;
        }

        var foodList = await _httpClient.GetFromJsonAsync<TacoFoodListResponseDto>(FoodListPath, cancellationToken);
        if (foodList is null)
        {
            throw new InvalidOperationException($"TACO returned an empty {FoodListPath}.");
        }

        _categoriesById = foodList.Foods.ToDictionary(summary => summary.Id, summary => summary.Category);
        return _categoriesById;
    }
}
