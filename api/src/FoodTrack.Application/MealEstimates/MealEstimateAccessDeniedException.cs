namespace FoodTrack.Application.MealEstimates;

public sealed class MealEstimateAccessDeniedException : Exception
{
    public MealEstimateAccessDeniedException()
        : base("AI meal estimates are not available for this account.")
    {
    }
}
