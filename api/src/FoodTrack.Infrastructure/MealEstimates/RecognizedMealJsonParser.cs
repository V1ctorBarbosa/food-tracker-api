using System.Text.Json;
using FoodTrack.Application.MealEstimates;

namespace FoodTrack.Infrastructure.MealEstimates;

/// <summary>Reads the estimator's JSON answer, shaped by <see cref="MealEstimatePrompt.BuildAnswerSchema"/>.</summary>
internal static class RecognizedMealJsonParser
{
    /// <exception cref="MealEstimatorUnavailableException">The text is not JSON of the expected shape.</exception>
    public static RecognizedMeal Parse(string answerJson)
    {
        try
        {
            using var document = JsonDocument.Parse(answerJson);
            var foods = document.RootElement
                .GetProperty(MealEstimatePrompt.FoodsProperty)
                .EnumerateArray()
                .Select(ToRecognizedFood)
                .ToList();

            return new RecognizedMeal(foods);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new MealEstimatorUnavailableException("The estimator's answer did not have the expected shape.", exception);
        }
    }

    private static RecognizedFood ToRecognizedFood(JsonElement food) => new(
        food.GetProperty(MealEstimatePrompt.NameProperty).GetString() ?? string.Empty,
        food.GetProperty(MealEstimatePrompt.QuantityProperty).GetDouble(),
        food.GetProperty(MealEstimatePrompt.BaseUnitProperty).GetString() ?? string.Empty,
        food.GetProperty(MealEstimatePrompt.CaloriesProperty).GetDouble(),
        food.GetProperty(MealEstimatePrompt.ProteinProperty).GetDouble(),
        food.GetProperty(MealEstimatePrompt.CarbsProperty).GetDouble(),
        food.GetProperty(MealEstimatePrompt.FatProperty).GetDouble());
}
