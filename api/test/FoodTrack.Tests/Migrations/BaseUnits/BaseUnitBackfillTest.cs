using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Migrations.BaseUnits;
using NSubstitute;

namespace FoodTrack.Tests.Migrations.BaseUnits;

public sealed class BaseUnitBackfillTest
{
    private readonly IFoodRepository _foodRepository = Substitute.For<IFoodRepository>();
    private readonly IMealRepository _mealRepository = Substitute.For<IMealRepository>();
    private readonly IFoodUnitClassifier _tacoClassifier = Substitute.For<IFoodUnitClassifier>();
    private readonly IFoodUnitClassifier _openFoodFactsClassifier = Substitute.For<IFoodUnitClassifier>();
    private readonly BaseUnitBackfill _sut;

    public BaseUnitBackfillTest()
    {
        _tacoClassifier.Source.Returns(FoodSource.Taco);
        _openFoodFactsClassifier.Source.Returns(FoodSource.OpenFoodFacts);
        _foodRepository.ListBySourceAsync(Arg.Any<FoodSource>(), Arg.Any<CancellationToken>()).Returns([]);
        _sut = new BaseUnitBackfill(_foodRepository, _mealRepository, [_tacoClassifier, _openFoodFactsClassifier]);
    }

    [Fact]
    public async Task RunAsync_MovesTheItemsAndThenTheFood_WhenADrinkIsStoredInGrams()
    {
        // Arrange
        var cocaCola = CreateFood("Coca-Cola", FoodSource.OpenFoodFacts, "7894900011708");
        GivenFoods(FoodSource.OpenFoodFacts, cocaCola);
        GivenClassification(_openFoodFactsClassifier, cocaCola, MeasurementUnit.Milliliter);

        // Act
        var report = await _sut.RunAsync(applyChanges: true);

        // Assert
        report.Changed.Should().ContainSingle().Which.Should().Be(new BaseUnitChange(cocaCola, MeasurementUnit.Milliliter));
        Received.InOrder(() =>
        {
            _mealRepository.ReclassifyItemUnitsAsync(cocaCola.Id, MeasurementUnitCategory.Volume, Arg.Any<CancellationToken>());
            _foodRepository.SaveAsync(
                Arg.Is<Food>(saved => saved.Id == cocaCola.Id && saved.BaseUnit == new BaseUnit(100, MeasurementUnit.Milliliter)),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunAsync_WritesNothing_OnADryRun()
    {
        // Arrange
        var milk = CreateFood("Leite, de vaca, integral", FoodSource.Taco, "458");
        GivenFoods(FoodSource.Taco, milk);
        GivenClassification(_tacoClassifier, milk, MeasurementUnit.Milliliter);

        // Act
        var report = await _sut.RunAsync(applyChanges: false);

        // Assert
        report.Changed.Should().ContainSingle();
        await _mealRepository.DidNotReceiveWithAnyArgs().ReclassifyItemUnitsAsync(default, default, default);
        await _foodRepository.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_LeavesAFoodAlone_WhenItAlreadyHasTheRightUnit()
    {
        // Arrange
        var rice = CreateFood("Arroz, integral, cozido", FoodSource.Taco, "1");
        GivenFoods(FoodSource.Taco, rice);
        GivenClassification(_tacoClassifier, rice, MeasurementUnit.Gram);

        // Act
        var report = await _sut.RunAsync(applyChanges: true);

        // Assert
        report.UnchangedCount.Should().Be(1);
        report.Changed.Should().BeEmpty();
        await _foodRepository.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_ReportsAndSkipsAFood_WhenItsSourceHasNoAnswer()
    {
        // Arrange
        var removedProduct = CreateFood("Suco descontinuado", FoodSource.OpenFoodFacts, "0000000000000");
        GivenFoods(FoodSource.OpenFoodFacts, removedProduct);
        _openFoodFactsClassifier.ClassifyAsync(removedProduct, Arg.Any<CancellationToken>()).Returns((MeasurementUnit?)null);

        // Act
        var report = await _sut.RunAsync(applyChanges: true);

        // Assert
        report.Unclassified.Should().ContainSingle().Which.Should().Be(removedProduct);
        await _mealRepository.DidNotReceiveWithAnyArgs().ReclassifyItemUnitsAsync(default, default, default);
        await _foodRepository.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_AsksEachClassifierOnlyAboutFoodsFromItsOwnSource()
    {
        // Arrange
        var milk = CreateFood("Leite, de vaca, integral", FoodSource.Taco, "458");
        var cocaCola = CreateFood("Coca-Cola", FoodSource.OpenFoodFacts, "7894900011708");
        GivenFoods(FoodSource.Taco, milk);
        GivenFoods(FoodSource.OpenFoodFacts, cocaCola);

        // Act
        await _sut.RunAsync(applyChanges: false);

        // Assert
        await _tacoClassifier.Received(1).ClassifyAsync(milk, Arg.Any<CancellationToken>());
        await _tacoClassifier.DidNotReceive().ClassifyAsync(cocaCola, Arg.Any<CancellationToken>());
        await _openFoodFactsClassifier.Received(1).ClassifyAsync(cocaCola, Arg.Any<CancellationToken>());
        await _foodRepository.DidNotReceive().ListBySourceAsync(FoodSource.Custom, Arg.Any<CancellationToken>());
    }

    private void GivenFoods(FoodSource source, params Food[] foods) =>
        _foodRepository.ListBySourceAsync(source, Arg.Any<CancellationToken>()).Returns(foods);

    private static void GivenClassification(IFoodUnitClassifier classifier, Food food, MeasurementUnit unit) =>
        classifier.ClassifyAsync(food, Arg.Any<CancellationToken>()).Returns(unit);

    private static Food CreateFood(string name, FoodSource source, string externalId) => Food.Create(
        FoodId.NewId(),
        name,
        source,
        externalId,
        new BaseUnit(100, MeasurementUnit.Gram),
        new Kilocalories(42),
        new Grams(0),
        new Grams(10.5),
        new Grams(0));
}
