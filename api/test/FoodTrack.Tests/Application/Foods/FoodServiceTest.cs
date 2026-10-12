using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;
using NSubstitute;

namespace FoodTrack.Tests.Application.Foods;

public sealed class FoodServiceTest
{
    private readonly IFoodRepository _foodRepository = Substitute.For<IFoodRepository>();
    private readonly IExternalFoodSource _externalFoodSource = Substitute.For<IExternalFoodSource>();
    private readonly IMealRepository _mealRepository = Substitute.For<IMealRepository>();
    private readonly FoodService _sut;
    private readonly UserId _userId = new("firebase-uid-123");
    private readonly UserId _otherUserId = new("firebase-uid-456");

    public FoodServiceTest()
    {
        _sut = new FoodService(_foodRepository, _externalFoodSource, _mealRepository);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenFoodDoesNotExist()
    {
        // Arrange
        var id = FoodId.NewId();
        _foodRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Food?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, _userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDto_WhenFoodExists()
    {
        // Arrange
        var food = Food.Create(
            FoodId.NewId(),
            "Coca-Cola",
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            new Kilocalories(42),
            new Grams(0),
            new Grams(10.6),
            new Grams(0));

        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.GetByIdAsync(food.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Coca-Cola");
        result.Source.Should().Be(nameof(FoodSource.Usda));
        result.BaseUnitQuantity.Should().Be(100);
        result.BaseUnitType.Should().Be(nameof(MeasurementUnit.Milliliter));
        result.CaloriesPerBaseUnit.Should().Be(42);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFoodHasNoOwner()
    {
        // Arrange
        var food = CreateChickenBreast();
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.GetByIdAsync(food.Id, _userId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenCallerOwnsTheFood()
    {
        // Arrange
        var food = CreateCustomFood(ownerId: _userId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.GetByIdAsync(food.Id, _userId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenFoodIsOwnedByAnotherUser()
    {
        // Arrange
        var food = CreateCustomFood(ownerId: _otherUserId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.GetByIdAsync(food.Id, _userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_ReturnsCachedResults_WithoutCallingExternalSource_WhenCacheHits()
    {
        // Arrange
        var food = CreateChickenBreast();
        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([food]);

        // Act
        var result = await _sut.SearchAsync(_userId, "chicken");

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Chicken breast");
        await _externalFoodSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_FallsBackToExternalSource_AndCachesResults_WhenCacheMisses()
    {
        // Arrange
        var food = CreateChickenBreast();
        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([]);
        _externalFoodSource.SearchAsync("chicken", Arg.Any<CancellationToken>()).Returns([food]);

        // Act
        var result = await _sut.SearchAsync(_userId, "chicken");

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Chicken breast");
        await _foodRepository.Received(1).SaveAsync(food, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ReusesExistingCachedFoodId_WhenExternalResultMatchesSourceAndExternalId()
    {
        // Arrange
        var externalFood = CreateChickenBreast();
        var existingCachedId = FoodId.NewId();
        var existingCachedFood = Food.Create(
            existingCachedId,
            "Chicken breast (stale name)",
            FoodSource.Usda,
            "171077",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(160),
            new Grams(30),
            new Grams(0),
            new Grams(3.5));

        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([]);
        _externalFoodSource.SearchAsync("chicken", Arg.Any<CancellationToken>()).Returns([externalFood]);
        _foodRepository.GetBySourceAndExternalIdAsync(FoodSource.Usda, "171077", Arg.Any<CancellationToken>())
            .Returns(existingCachedFood);

        // Act
        var result = await _sut.SearchAsync(_userId, "chicken");

        // Assert -- fresh nutrition data is kept, but the id is the one already cached, so a
        // repeated search for the same external food never duplicates its cache entry.
        result.Should().ContainSingle();
        result[0].Id.Should().Be(existingCachedId.Value);
        result[0].Name.Should().Be("Chicken breast");
        await _foodRepository.Received(1).SaveAsync(
            Arg.Is<Food>(food => food.Id == existingCachedId && food.Name == "Chicken breast"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenBothCacheAndExternalSourceMiss()
    {
        // Arrange
        _foodRepository.SearchByNameAsync("nonexistent", Arg.Any<CancellationToken>()).Returns([]);
        _externalFoodSource.SearchAsync("nonexistent", Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _sut.SearchAsync(_userId, "nonexistent");

        // Assert
        result.Should().BeEmpty();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ExcludesCachedFoods_OwnedByAnotherUser()
    {
        // Arrange
        var ownFood = CreateCustomFood(ownerId: _userId, name: "My protein shake");
        var othersFood = CreateCustomFood(ownerId: _otherUserId, name: "Their protein shake");
        _foodRepository.SearchByNameAsync("protein", Arg.Any<CancellationToken>()).Returns([ownFood, othersFood]);
        _externalFoodSource.SearchAsync("protein", Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _sut.SearchAsync(_userId, "protein");

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("My protein shake");
    }

    [Fact]
    public async Task SearchAsync_StillQueriesTheExternalSource_WhenTheOnlyCachedMatchIsAnotherUsersCustomFood()
    {
        // Arrange -- another user named a custom food after the term before anyone searched it.
        var othersFood = CreateCustomFood(ownerId: _otherUserId, name: "Chicken breast");
        var externalFood = CreateChickenBreast();
        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([othersFood]);
        _externalFoodSource.SearchAsync("chicken", Arg.Any<CancellationToken>()).Returns([externalFood]);

        // Act
        var result = await _sut.SearchAsync(_userId, "chicken");

        // Assert
        result.Should().ContainSingle().Which.Id.Should().Be(externalFood.Id.Value);
        await _foodRepository.Received(1).SaveAsync(externalFood, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ReturnsOwnCustomFoodsAlongsideExternalResults_WhenNoSharedFoodIsCached()
    {
        // Arrange
        var ownFood = CreateCustomFood(ownerId: _userId, name: "My chicken salad");
        var externalFood = CreateChickenBreast();
        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([ownFood]);
        _externalFoodSource.SearchAsync("chicken", Arg.Any<CancellationToken>()).Returns([externalFood]);

        // Act
        var result = await _sut.SearchAsync(_userId, "chicken");

        // Assert
        result.Select(food => food.Name).Should().BeEquivalentTo("My chicken salad", "Chicken breast");
    }

    [Fact]
    public async Task SearchAsync_DoesNotQueryTheExternalSource_WhenASharedFoodIsCachedNextToCustomFoods()
    {
        // Arrange
        var sharedFood = CreateChickenBreast();
        var ownFood = CreateCustomFood(ownerId: _userId, name: "My chicken salad");
        var othersFood = CreateCustomFood(ownerId: _otherUserId, name: "Their chicken wrap");
        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([sharedFood, ownFood, othersFood]);

        // Act
        var result = await _sut.SearchAsync(_userId, "chicken");

        // Assert
        result.Select(food => food.Name).Should().BeEquivalentTo("Chicken breast", "My chicken salad");
        await _externalFoodSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("ab")]
    public async Task SearchAsync_Throws_WhenQueryIsShorterThanMinimumLength(string query)
    {
        // Act
        var act = async () => await _sut.SearchAsync(_userId, query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _foodRepository.DidNotReceive().SearchByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _externalFoodSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_Throws_WhenQueryIsLongerThanMaximumLength()
    {
        // Arrange
        var query = new string('a', 101);

        // Act
        var act = async () => await _sut.SearchAsync(_userId, query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _foodRepository.DidNotReceive().SearchByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _externalFoodSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomAsync_Throws_AndSavesNothing_WhenTheNameIsLongerThanTheMaximumLength()
    {
        // Arrange
        var request = new CreateCustomFoodRequest(new string('a', 101), 120, 24, 3, 1.5);

        // Act
        var act = () => _sut.CreateCustomAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomAsync_SavesFood_WithSourceCustomAndOwnerSet()
    {
        // Arrange
        var request = new CreateCustomFoodRequest("My whey", 120, 24, 3, 1.5);

        // Act
        var result = await _sut.CreateCustomAsync(_userId, request);

        // Assert
        result.Name.Should().Be("My whey");
        result.Source.Should().Be(nameof(FoodSource.Custom));
        result.BaseUnitQuantity.Should().Be(100);
        result.BaseUnitType.Should().Be(nameof(MeasurementUnit.Gram));
        result.CaloriesPerBaseUnit.Should().Be(120);
        result.ProteinPerBaseUnit.Should().Be(24);
        result.CarbsPerBaseUnit.Should().Be(3);
        result.FatPerBaseUnit.Should().Be(1.5);
        await _foodRepository.Received(1).SaveAsync(
            Arg.Is<Food>(food => food.OwnerId == _userId && food.Source == FoodSource.Custom),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCustomAsync_ReturnsTheUsersCustomFoods_OrderedByNameIgnoringCase()
    {
        // Arrange
        var granola = CreateCustomFood(_userId, "granola caseira");
        var friedBanana = CreateCustomFood(_userId, "Banana frita");
        var oats = CreateCustomFood(_userId, "Aveia");
        _foodRepository.ListCustomFoodsOwnedByAsync(_userId, Arg.Any<CancellationToken>()).Returns([granola, friedBanana, oats]);

        // Act
        var result = await _sut.ListCustomAsync(_userId);

        // Assert
        result.Select(food => food.Name).Should().Equal("Aveia", "Banana frita", "granola caseira");
    }

    [Fact]
    public async Task ListCustomAsync_ReturnsAnEmptyList_WhenTheUserHasNoCustomFoods()
    {
        // Arrange
        _foodRepository.ListCustomFoodsOwnedByAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _sut.ListCustomAsync(_userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListCustomAsync_AsksOnlyForTheCallersOwnFoods()
    {
        // Arrange
        _foodRepository.ListCustomFoodsOwnedByAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.ListCustomAsync(_userId);

        // Assert
        await _foodRepository.Received(1).ListCustomFoodsOwnedByAsync(_userId, Arg.Any<CancellationToken>());
        await _foodRepository.DidNotReceive().ListCustomFoodsOwnedByAsync(_otherUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_ReplacesNameAndNutrition_KeepingIdSourceAndOwner()
    {
        // Arrange
        var food = CreateCustomFood(_userId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);
        var request = new UpdateCustomFoodRequest("  Whey baunilha ", 130, 25, 4, 2);

        // Act
        var result = await _sut.UpdateCustomAsync(_userId, food.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(food.Id.Value);
        result.Name.Should().Be("Whey baunilha");
        result.Source.Should().Be(nameof(FoodSource.Custom));
        result.BaseUnitQuantity.Should().Be(100);
        result.CaloriesPerBaseUnit.Should().Be(130);
        result.ProteinPerBaseUnit.Should().Be(25);
        result.CarbsPerBaseUnit.Should().Be(4);
        result.FatPerBaseUnit.Should().Be(2);
        await _foodRepository.Received(1).SaveAsync(
            Arg.Is<Food>(saved => saved.Id == food.Id && saved.OwnerId == _userId && saved.Source == FoodSource.Custom),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_ReturnsNull_AndSavesNothing_WhenTheFoodDoesNotExist()
    {
        // Arrange
        var id = FoodId.NewId();
        _foodRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Food?)null);

        // Act
        var result = await _sut.UpdateCustomAsync(_userId, id, new UpdateCustomFoodRequest("Whey", 130, 25, 4, 2));

        // Assert
        result.Should().BeNull();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_ReturnsNull_AndSavesNothing_WhenTheFoodBelongsToAnotherUser()
    {
        // Arrange
        var othersFood = CreateCustomFood(_otherUserId);
        _foodRepository.GetByIdAsync(othersFood.Id, Arg.Any<CancellationToken>()).Returns(othersFood);

        // Act
        var result = await _sut.UpdateCustomAsync(_userId, othersFood.Id, new UpdateCustomFoodRequest("Mine now", 1, 1, 1, 1));

        // Assert
        result.Should().BeNull();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_ReturnsNull_AndSavesNothing_ForASharedFood()
    {
        // Arrange
        var sharedFood = CreateChickenBreast();
        _foodRepository.GetByIdAsync(sharedFood.Id, Arg.Any<CancellationToken>()).Returns(sharedFood);

        // Act
        var result = await _sut.UpdateCustomAsync(_userId, sharedFood.Id, new UpdateCustomFoodRequest("Edited", 1, 1, 1, 1));

        // Assert
        result.Should().BeNull();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_Throws_AndSavesNothing_WhenTheNameIsLongerThanTheMaximumLength()
    {
        // Arrange
        var food = CreateCustomFood(_userId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var act = () => _sut.UpdateCustomAsync(_userId, food.Id, new UpdateCustomFoodRequest(new string('a', 101), 120, 24, 3, 1.5));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCustomAsync_Throws_AndSavesNothing_WhenTheNameIsBlank(string blankName)
    {
        // Arrange
        var food = CreateCustomFood(_userId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var act = () => _sut.UpdateCustomAsync(_userId, food.Id, new UpdateCustomFoodRequest(blankName, 120, 24, 3, 1.5));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomAsync_SavesADrink_PerHundredMilliliters_WhenTheBaseUnitIsMilliliter()
    {
        // Arrange
        var request = new CreateCustomFoodRequest("Suco verde", 45, 0.5, 10, 0.1, "Milliliter");

        // Act
        var result = await _sut.CreateCustomAsync(_userId, request);

        // Assert
        result.BaseUnitQuantity.Should().Be(100);
        result.BaseUnitType.Should().Be(nameof(MeasurementUnit.Milliliter));
        await _foodRepository.Received(1).SaveAsync(
            Arg.Is<Food>(food => food.BaseUnit == new BaseUnit(100, MeasurementUnit.Milliliter)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Liter")]
    [InlineData("Kilogram")]
    [InlineData("cup")]
    public async Task CreateCustomAsync_Throws_AndSavesNothing_WhenTheBaseUnitIsNotGramOrMilliliter(string baseUnitType)
    {
        // Arrange
        var request = new CreateCustomFoodRequest("Suco verde", 45, 0.5, 10, 0.1, baseUnitType);

        // Act
        var act = () => _sut.CreateCustomAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_MovesTheLoggedItemsToVolume_BeforeSaving_WhenAFoodBecomesADrink()
    {
        // Arrange
        var food = CreateCustomFood(_userId, "Suco verde");
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);
        var request = new UpdateCustomFoodRequest("Suco verde", 45, 0.5, 10, 0.1, "Milliliter");

        // Act
        var result = await _sut.UpdateCustomAsync(_userId, food.Id, request);

        // Assert
        result!.BaseUnitType.Should().Be(nameof(MeasurementUnit.Milliliter));
        Received.InOrder(() =>
        {
            _mealRepository.ReclassifyItemUnitsAsync(food.Id, MeasurementUnitCategory.Volume, Arg.Any<CancellationToken>());
            _foodRepository.SaveAsync(
                Arg.Is<Food>(saved => saved.BaseUnit == new BaseUnit(100, MeasurementUnit.Milliliter)),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task UpdateCustomAsync_MovesTheLoggedItemsToMass_WhenADrinkBecomesAFood()
    {
        // Arrange
        var drink = CreateCustomFood(_userId, "Iogurte batido", MeasurementUnit.Milliliter);
        _foodRepository.GetByIdAsync(drink.Id, Arg.Any<CancellationToken>()).Returns(drink);
        var request = new UpdateCustomFoodRequest("Iogurte batido", 70, 4, 9, 2, "Gram");

        // Act
        await _sut.UpdateCustomAsync(_userId, drink.Id, request);

        // Assert
        await _mealRepository.Received(1).ReclassifyItemUnitsAsync(drink.Id, MeasurementUnitCategory.Mass, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomAsync_KeepsTheCurrentUnit_AndLeavesLoggedItemsAlone_WhenTheBaseUnitIsLeftOut()
    {
        // Arrange
        var drink = CreateCustomFood(_userId, "Suco verde", MeasurementUnit.Milliliter);
        _foodRepository.GetByIdAsync(drink.Id, Arg.Any<CancellationToken>()).Returns(drink);
        var request = new UpdateCustomFoodRequest("Suco verde detox", 40, 0.5, 9, 0.1);

        // Act
        var result = await _sut.UpdateCustomAsync(_userId, drink.Id, request);

        // Assert
        result!.BaseUnitType.Should().Be(nameof(MeasurementUnit.Milliliter));
        await _mealRepository.DidNotReceiveWithAnyArgs().ReclassifyItemUnitsAsync(default, default, default);
    }

    [Fact]
    public async Task UpdateCustomAsync_LeavesLoggedItemsAlone_WhenTheBaseUnitDoesNotChange()
    {
        // Arrange
        var food = CreateCustomFood(_userId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);
        var request = new UpdateCustomFoodRequest("My whey", 120, 24, 3, 1.5, "Gram");

        // Act
        await _sut.UpdateCustomAsync(_userId, food.Id, request);

        // Assert
        await _mealRepository.DidNotReceiveWithAnyArgs().ReclassifyItemUnitsAsync(default, default, default);
    }

    [Fact]
    public async Task UpdateCustomAsync_LeavesLoggedItemsAlone_WhenTheFoodBelongsToAnotherUser()
    {
        // Arrange
        var othersFood = CreateCustomFood(_otherUserId);
        _foodRepository.GetByIdAsync(othersFood.Id, Arg.Any<CancellationToken>()).Returns(othersFood);
        var request = new UpdateCustomFoodRequest("Mine now", 1, 1, 1, 1, "Milliliter");

        // Act
        await _sut.UpdateCustomAsync(_userId, othersFood.Id, request);

        // Assert
        await _mealRepository.DidNotReceiveWithAnyArgs().ReclassifyItemUnitsAsync(default, default, default);
    }

    [Fact]
    public async Task UpdateCustomAsync_Throws_AndChangesNothing_WhenTheBaseUnitIsNotGramOrMilliliter()
    {
        // Arrange
        var food = CreateCustomFood(_userId);
        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);
        var request = new UpdateCustomFoodRequest("My whey", 120, 24, 3, 1.5, "Liter");

        // Act
        var act = () => _sut.UpdateCustomAsync(_userId, food.Id, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _mealRepository.DidNotReceiveWithAnyArgs().ReclassifyItemUnitsAsync(default, default, default);
        await _foodRepository.DidNotReceive().SaveAsync(Arg.Any<Food>(), Arg.Any<CancellationToken>());
    }

    private static Food CreateChickenBreast() => Food.Create(
        FoodId.NewId(),
        "Chicken breast",
        FoodSource.Usda,
        "171077",
        new BaseUnit(100, MeasurementUnit.Gram),
        new Kilocalories(165),
        new Grams(31),
        new Grams(0),
        new Grams(3.6));

    private static Food CreateCustomFood(
        UserId ownerId,
        string name = "My whey",
        MeasurementUnit baseUnit = MeasurementUnit.Gram) => Food.Create(
        FoodId.NewId(),
        name,
        FoodSource.Custom,
        externalId: null,
        new BaseUnit(100, baseUnit),
        new Kilocalories(120),
        new Grams(24),
        new Grams(3),
        new Grams(1.5),
        ownerId: ownerId);
}
