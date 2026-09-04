using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using NSubstitute;

namespace FoodTrack.Tests.Application.Meals;

public sealed class MealServiceTest
{
    private readonly IMealRepository _mealRepository = Substitute.For<IMealRepository>();
    private readonly IFoodRepository _foodRepository = Substitute.For<IFoodRepository>();
    private readonly MealService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public MealServiceTest()
    {
        _sut = new MealService(_mealRepository, _foodRepository);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_WithTotals()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 7);
        var meal = CreateMeal([CreateItem(260, 5.4, 56, 0.6)]);
        _mealRepository.ListByUserAsync(_userId, from, to, Arg.Any<CancellationToken>()).Returns([meal]);

        // Act
        var result = await _sut.ListAsync(_userId, from, to);

        // Assert
        result.Should().ContainSingle();
        result[0].TotalCalories.Should().Be(260);
        result[0].Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMealDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = MealId.NewId();
        _mealRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((Meal?)null);

        // Act
        var result = await _sut.GetByIdAsync(_userId, id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ResolvesItem_FromQuantityAndUnit_UsingNutritionCalculator()
    {
        // Arrange — docs section 18: rice at 130 kcal/100g, 200g -> 260 kcal.
        var rice = CreateFood("Rice", 130, 2.7, 28, 0.3);
        _foodRepository.GetByIdAsync(rice.Id, Arg.Any<CancellationToken>()).Returns(rice);

        var request = new CreateMealRequest(
            nameof(MealType.Lunch),
            [new MealItemRequest(rice.Id.Value, 200, nameof(MeasurementUnit.Gram), null, null)]);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Calories.Should().Be(260);
        result.Items[0].Quantity.Should().Be(200);
        result.TotalCalories.Should().Be(260);
        await _mealRepository.Received(1).AddAsync(Arg.Is<Meal>(meal => meal.UserId == _userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ResolvesItem_FromFoodServing_UsingNutritionCalculator()
    {
        // Arrange — docs section 17: Coca-Cola at 42 kcal/100ml, 1 can (350ml) -> 147 kcal.
        var canServing = FoodServing.Create(FoodServingId.NewId(), "can", 350, MeasurementUnit.Milliliter);
        var cocaCola = Food.Create(
            FoodId.NewId(),
            "Coca-Cola",
            FoodSource.Usda,
            "67890",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            new Kilocalories(42),
            new Grams(0),
            new Grams(10.6),
            new Grams(0),
            [canServing]);

        _foodRepository.GetByIdAsync(cocaCola.Id, Arg.Any<CancellationToken>()).Returns(cocaCola);

        var request = new CreateMealRequest(
            nameof(MealType.Snack),
            [new MealItemRequest(cocaCola.Id.Value, null, null, canServing.Id.Value, 1)]);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Calories.Should().Be(147);
        result.Items[0].Quantity.Should().Be(350);
        result.Items[0].Unit.Should().Be(nameof(MeasurementUnit.Milliliter));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenFoodDoesNotExist()
    {
        // Arrange
        var foodId = FoodId.NewId();
        _foodRepository.GetByIdAsync(foodId, Arg.Any<CancellationToken>()).Returns((Food?)null);

        var request = new CreateMealRequest(
            nameof(MealType.Lunch),
            [new MealItemRequest(foodId.Value, 200, nameof(MeasurementUnit.Gram), null, null)]);

        // Act
        var act = async () => await _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _mealRepository.DidNotReceive().AddAsync(Arg.Any<Meal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNeitherQuantityNorServingIsProvided()
    {
        // Arrange
        var rice = CreateFood("Rice", 130, 2.7, 28, 0.3);
        _foodRepository.GetByIdAsync(rice.Id, Arg.Any<CancellationToken>()).Returns(rice);

        var request = new CreateMealRequest(
            nameof(MealType.Lunch),
            [new MealItemRequest(rice.Id.Value, null, null, null, null)]);

        // Act
        var act = async () => await _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenFoodServingIdDoesNotBelongToFood()
    {
        // Arrange
        var rice = CreateFood("Rice", 130, 2.7, 28, 0.3);
        _foodRepository.GetByIdAsync(rice.Id, Arg.Any<CancellationToken>()).Returns(rice);

        var request = new CreateMealRequest(
            nameof(MealType.Lunch),
            [new MealItemRequest(rice.Id.Value, null, null, FoodServingId.NewId().Value, 1)]);

        // Act
        var act = async () => await _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenMealDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = MealId.NewId();
        _mealRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((Meal?)null);

        var rice = CreateFood("Rice", 130, 2.7, 28, 0.3);
        var request = new UpdateMealRequest(
            nameof(MealType.Lunch),
            [new MealItemRequest(rice.Id.Value, 200, nameof(MeasurementUnit.Gram), null, null)]);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().BeNull();
        await _mealRepository.DidNotReceive().UpdateAsync(Arg.Any<Meal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReplacesMeal_WhenOwnedByCaller()
    {
        // Arrange
        var id = MealId.NewId();
        var existing = CreateMeal([CreateItem(260, 5.4, 56, 0.6)], id);
        _mealRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        var rice = CreateFood("Rice", 130, 2.7, 28, 0.3);
        _foodRepository.GetByIdAsync(rice.Id, Arg.Any<CancellationToken>()).Returns(rice);

        var request = new UpdateMealRequest(
            nameof(MealType.Dinner),
            [new MealItemRequest(rice.Id.Value, 100, nameof(MeasurementUnit.Gram), null, null)]);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().NotBeNull();
        result!.MealType.Should().Be(nameof(MealType.Dinner));
        result.TotalCalories.Should().Be(130);
        result.LoggedAt.Should().Be(existing.LoggedAt);
        await _mealRepository.Received(1).UpdateAsync(
            Arg.Is<Meal>(meal => meal.Id == id && meal.LoggedAt == existing.LoggedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenMealDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = MealId.NewId();
        _mealRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((Meal?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeFalse();
        await _mealRepository.DidNotReceive().DeleteAsync(Arg.Any<MealId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenOwnedByCaller()
    {
        // Arrange
        var id = MealId.NewId();
        var existing = CreateMeal([CreateItem(260, 5.4, 56, 0.6)], id);
        _mealRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeTrue();
        await _mealRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    private Meal CreateMeal(IReadOnlyList<MealItem> items, MealId? id = null) => Meal.Create(
        id ?? MealId.NewId(),
        _userId,
        new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero),
        MealType.Lunch,
        items);

    private static MealItem CreateItem(int calories, double protein, double carbs, double fat) => MealItem.Create(
        MealItemId.NewId(),
        FoodId.NewId(),
        200,
        MeasurementUnit.Gram,
        new Kilocalories(calories),
        new Grams(protein),
        new Grams(carbs),
        new Grams(fat));

    private static Food CreateFood(string name, int caloriesPer100g, double proteinPer100g, double carbsPer100g, double fatPer100g) =>
        Food.Create(
            FoodId.NewId(),
            name,
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(caloriesPer100g),
            new Grams(proteinPer100g),
            new Grams(carbsPer100g),
            new Grams(fatPer100g));
}
