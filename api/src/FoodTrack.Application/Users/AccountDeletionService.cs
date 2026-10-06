using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Users;

/// <summary>
/// Deletes a user's account and everything they logged, as data protection law and the app
/// stores require. Every step is idempotent, so a deletion that fails partway can simply be
/// requested again.
/// </summary>
public sealed class AccountDeletionService
{
    private readonly IMealRepository _mealRepository;
    private readonly IWaterEntryRepository _waterEntryRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly IFoodRepository _foodRepository;
    private readonly IUserRepository _userRepository;
    private readonly IIdentityAccountRemover _identityAccountRemover;

    public AccountDeletionService(
        IMealRepository mealRepository,
        IWaterEntryRepository waterEntryRepository,
        IWeightEntryRepository weightEntryRepository,
        IFoodRepository foodRepository,
        IUserRepository userRepository,
        IIdentityAccountRemover identityAccountRemover)
    {
        _mealRepository = mealRepository;
        _waterEntryRepository = waterEntryRepository;
        _weightEntryRepository = weightEntryRepository;
        _foodRepository = foodRepository;
        _userRepository = userRepository;
        _identityAccountRemover = identityAccountRemover;
    }

    /// <summary>
    /// The sign-in account goes last: while it still exists the user can sign in and retry, so a
    /// failure partway never leaves data behind that nobody can reach anymore.
    /// </summary>
    public async Task DeleteAccountAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await _mealRepository.DeleteAllByUserAsync(userId, cancellationToken);
        await _waterEntryRepository.DeleteAllByUserAsync(userId, cancellationToken);
        await _weightEntryRepository.DeleteAllByUserAsync(userId, cancellationToken);
        await _foodRepository.DeleteCustomFoodsOwnedByAsync(userId, cancellationToken);
        await _userRepository.DeleteAsync(userId, cancellationToken);
        await _identityAccountRemover.RemoveAsync(userId, cancellationToken);
    }
}
