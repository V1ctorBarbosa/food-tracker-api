using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Users;

public sealed class UserProfileService
{
    private readonly IUserRepository _userRepository;

    public UserProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto?> GetProfileAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserProfileDto> UpsertProfileAsync(
        UserId userId,
        UpsertUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = User.Create(
            userId,
            request.Name,
            new Age(request.Age),
            new Centimeters(request.HeightCentimeters),
            Enum.Parse<Sex>(request.Sex, ignoreCase: true),
            Enum.Parse<ActivityLevel>(request.ActivityLevel, ignoreCase: true),
            new Kilocalories(request.DailyCalorieTarget),
            new Kilocalories(request.DesiredCalorieDeficit),
            request.TargetWeightKilograms is { } weight ? new Kilograms(weight) : null);

        await _userRepository.SaveAsync(user, cancellationToken);
        return ToDto(user);
    }

    private static UserProfileDto ToDto(User user) => new(
        user.Id.Value,
        user.Name,
        user.Age.Value,
        user.Height.Value,
        user.Sex.ToString(),
        user.ActivityLevel.ToString(),
        user.TargetWeight?.Value,
        user.DesiredCalorieDeficit.Value,
        user.DailyCalorieTarget.Value);
}
