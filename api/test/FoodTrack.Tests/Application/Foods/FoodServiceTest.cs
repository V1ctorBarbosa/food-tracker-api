using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;
using NSubstitute;

namespace FoodTrack.Tests.Application.Foods;

public sealed class FoodServiceTest
{
    private readonly IFoodRepository _foodRepository = Substitute.For<IFoodRepository>();
    private readonly IExternalFoodSource _externalFoodSource = Substitute.For<IExternalFoodSource>();
    private readonly FoodService _sut;
    private readonly UserId _userId = new("firebase-uid-123");
    private readonly UserId _otherUserId = new("firebase-uid-456");

    public FoodServiceTest()
    {
        _sut = new FoodService(_foodRepository, _externalFoodSource);
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
    public async Task GetByIdAsync_ReturnsMappedDto_WithServings_WhenFoodExists()
    {
        // Arrange
        var servingId = FoodServingId.NewId();
        var serving = FoodServing.Create(servingId, "can", 350, MeasurementUnit.Milliliter);
        var food = Food.Create(
            FoodId.NewId(),
            "Coca-Cola",
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            new Kilocalories(42),
            new Grams(0),
            new Grams(10.6),
            new Grams(0),
            [serving]);

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
        result.Servings.Should().ContainSingle();
        result.Servings[0].Id.Should().Be(servingId.Value);
        result.Servings[0].Name.Should().Be("can");
        result.Servings[0].Quantity.Should().Be(350);
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

        // Act
        var result = await _sut.SearchAsync(_userId, "protein");

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("My protein shake");
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

    private static Food CreateCustomFood(UserId ownerId, string name = "My whey") => Food.Create(
        FoodId.NewId(),
        name,
        FoodSource.Custom,
        externalId: null,
        new BaseUnit(100, MeasurementUnit.Gram),
        new Kilocalories(120),
        new Grams(24),
        new Grams(3),
        new Grams(1.5),
        ownerId: ownerId);
}
