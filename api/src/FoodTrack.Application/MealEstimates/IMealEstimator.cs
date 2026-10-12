namespace FoodTrack.Application.MealEstimates;

/// <summary>The AI that recognizes the foods in a meal and estimates their amounts and nutrition.</summary>
public interface IMealEstimator
{
    /// <exception cref="MealEstimatorUnavailableException">The estimator could not be reached or gave no usable answer.</exception>
    Task<RecognizedMeal> EstimateAsync(MealEstimateInput input, CancellationToken cancellationToken = default);
}
