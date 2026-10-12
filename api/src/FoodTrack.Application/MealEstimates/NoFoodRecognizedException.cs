namespace FoodTrack.Application.MealEstimates;

public sealed class NoFoodRecognizedException : Exception
{
    public NoFoodRecognizedException()
        : base("No food was recognized in this meal.")
    {
    }
}
