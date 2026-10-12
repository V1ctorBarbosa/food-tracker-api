namespace FoodTrack.Application.MealEstimates;

public sealed class MealEstimatorUnavailableException : Exception
{
    public MealEstimatorUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
