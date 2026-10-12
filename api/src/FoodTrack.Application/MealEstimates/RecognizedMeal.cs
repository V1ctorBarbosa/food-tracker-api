namespace FoodTrack.Application.MealEstimates;

/// <summary>
/// The estimator's raw answer, before the domain checks it. An empty <see cref="Foods"/> list
/// means it found no food at all.
/// </summary>
public sealed record RecognizedMeal(IReadOnlyList<RecognizedFood> Foods);
