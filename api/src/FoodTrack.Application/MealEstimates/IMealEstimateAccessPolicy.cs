using FoodTrack.Domain.Users;

namespace FoodTrack.Application.MealEstimates;

/// <summary>Decides who may use AI estimates, which are paid for by the app owner on every call.</summary>
public interface IMealEstimateAccessPolicy
{
    bool CanEstimate(UserId userId);
}
