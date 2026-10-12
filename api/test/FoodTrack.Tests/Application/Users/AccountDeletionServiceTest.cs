using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Application.Meals;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.Users;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FoodTrack.Tests.Application.Users;

public sealed class AccountDeletionServiceTest
{
    private readonly IMealRepository _mealRepository = Substitute.For<IMealRepository>();
    private readonly IWaterEntryRepository _waterEntryRepository = Substitute.For<IWaterEntryRepository>();
    private readonly IWeightEntryRepository _weightEntryRepository = Substitute.For<IWeightEntryRepository>();
    private readonly IFoodRepository _foodRepository = Substitute.For<IFoodRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityAccountRemover _identityAccountRemover = Substitute.For<IIdentityAccountRemover>();
    private readonly IMealPhotoStorage _mealPhotoStorage = Substitute.For<IMealPhotoStorage>();
    private readonly AccountDeletionService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public AccountDeletionServiceTest()
    {
        _sut = new AccountDeletionService(
            _mealRepository,
            _waterEntryRepository,
            _weightEntryRepository,
            _foodRepository,
            _userRepository,
            _identityAccountRemover,
            _mealPhotoStorage);
    }

    [Fact]
    public async Task DeleteAccountAsync_DeletesTheMealPhotos_BeforeTheMeals()
    {
        // Act
        await _sut.DeleteAccountAsync(_userId);

        // Assert
        Received.InOrder(() =>
        {
            _mealPhotoStorage.DeleteAllAsync(_userId, Arg.Any<CancellationToken>());
            _mealRepository.DeleteAllByUserAsync(_userId, Arg.Any<CancellationToken>());
            _identityAccountRemover.RemoveAsync(_userId, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task DeleteAccountAsync_DeletesEveryPieceOfUserData_AndTheSignInAccountLast()
    {
        // Act
        await _sut.DeleteAccountAsync(_userId);

        // Assert
        Received.InOrder(() =>
        {
            _mealRepository.DeleteAllByUserAsync(_userId, Arg.Any<CancellationToken>());
            _waterEntryRepository.DeleteAllByUserAsync(_userId, Arg.Any<CancellationToken>());
            _weightEntryRepository.DeleteAllByUserAsync(_userId, Arg.Any<CancellationToken>());
            _foodRepository.DeleteCustomFoodsOwnedByAsync(_userId, Arg.Any<CancellationToken>());
            _userRepository.DeleteAsync(_userId, Arg.Any<CancellationToken>());
            _identityAccountRemover.RemoveAsync(_userId, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task DeleteAccountAsync_KeepsTheSignInAccount_WhenDeletingTheDataFails()
    {
        // Arrange
        _weightEntryRepository
            .DeleteAllByUserAsync(_userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Firestore unavailable"));

        // Act
        var act = () => _sut.DeleteAccountAsync(_userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _identityAccountRemover.DidNotReceive().RemoveAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }
}
