namespace FoodTrack.Application.MealEstimates;

public sealed class MonthlyEstimateLimitReachedException : Exception
{
    public MonthlyEstimateLimitReachedException()
        : base("The monthly limit of AI meal estimates has been reached. Estimates come back on the first day of next month.")
    {
    }
}
