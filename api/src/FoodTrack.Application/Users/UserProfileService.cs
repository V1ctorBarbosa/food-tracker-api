using FoodTrack.Application.MealEstimates;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Users;

public sealed class UserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IMealEstimateAccessPolicy _mealEstimateAccessPolicy;

    public UserProfileService(IUserRepository userRepository, IMealEstimateAccessPolicy mealEstimateAccessPolicy)
    {
        _userRepository = userRepository;
        _mealEstimateAccessPolicy = mealEstimateAccessPolicy;
    }

    public async Task<UserProfileDto?> GetProfileAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return ToDto(user);
    }

    public async Task<UserProfileDto> UpsertProfileAsync(
        UserId userId,
        UpsertUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        Kilograms? targetWeight = null;
        if (request.TargetWeightKilograms is { } weight)
        {
            targetWeight = new Kilograms(weight);
        }

        Milliliters? dailyWaterTarget = null;
        if (request.DailyWaterTargetMilliliters is { } water)
        {
            dailyWaterTarget = new Milliliters(water);
        }

        Grams? dailyProteinTarget = null;
        if (request.DailyProteinTargetGrams is { } protein)
        {
            dailyProteinTarget = new Grams(protein);
        }

        Grams? dailyCarbsTarget = null;
        if (request.DailyCarbsTargetGrams is { } carbs)
        {
            dailyCarbsTarget = new Grams(carbs);
        }

        Grams? dailyFatTarget = null;
        if (request.DailyFatTargetGrams is { } fat)
        {
            dailyFatTarget = new Grams(fat);
        }

        var user = User.Create(
            userId,
            request.Name,
            new Age(request.Age),
            new Centimeters(request.HeightCentimeters),
            Enum.Parse<Gender>(request.Gender, ignoreCase: true),
            Enum.Parse<ActivityLevel>(request.ActivityLevel, ignoreCase: true),
            new Kilocalories(request.DailyCalorieTarget),
            new Kilocalories(request.DesiredCalorieDeficit),
            targetWeight,
            dailyWaterTarget,
            dailyProteinTarget,
            dailyCarbsTarget,
            dailyFatTarget);

        await _userRepository.SaveAsync(user, cancellationToken);
        return ToDto(user);
    }

    private UserProfileDto ToDto(User user) => new(
        user.Id.Value,
        user.Name,
        user.Age.Value,
        user.Height.Value,
        user.Gender.ToString(),
        user.ActivityLevel.ToString(),
        user.TargetWeight?.Value,
        user.DesiredCalorieDeficit.Value,
        user.DailyCalorieTarget.Value,
        user.DailyWaterTarget?.Value,
        user.DailyProteinTarget?.Value,
        user.DailyCarbsTarget?.Value,
        user.DailyFatTarget?.Value,
        _mealEstimateAccessPolicy.CanEstimate(user.Id));
}
