using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Tests.Domain.Foods;

public sealed class NutritionCalculatorTest
{
    [Fact]
    public void Calculate_ScalesByQuantity_ForRice200gExample()
    {
        // Arrange — docs section 19: rice at 130 kcal/100g, user logs 200g -> 260 kcal.
        var rice = Food.Create(
            FoodId.NewId(),
            "Rice",
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(130),
            new Grams(2.7),
            new Grams(28),
            new Grams(0.3));

        // Act
        var result = NutritionCalculator.Calculate(rice, 200, MeasurementUnit.Gram);

        // Assert
        result.Calories.Value.Should().Be(260);
        result.Protein.Value.Should().Be(5.4);
        result.Carbs.Value.Should().Be(56);
        result.Fat.Value.Should().Be(0.6);
    }

    [Fact]
    public void Calculate_ConvertsKilogramToGram_BeforeScaling()
    {
        // Arrange
        var rice = Food.Create(
            FoodId.NewId(),
            "Rice",
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(130),
            new Grams(2.7),
            new Grams(28),
            new Grams(0.3));

        // Act — 0.2 kg is the same as 200g.
        var result = NutritionCalculator.Calculate(rice, 0.2, MeasurementUnit.Kilogram);

        // Assert
        result.Calories.Value.Should().Be(260);
    }

    [Fact]
    public void Calculate_Throws_WhenQuantityUnitCrossesMassAndVolume()
    {
        // Arrange
        var rice = Food.Create(
            FoodId.NewId(),
            "Rice",
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(130),
            new Grams(2.7),
            new Grams(28),
            new Grams(0.3));

        // Act
        var act = () => NutritionCalculator.Calculate(rice, 200, MeasurementUnit.Milliliter);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calculate_ByServing_ResolvesQuantity_ForCocaColaCanExample()
    {
        // Arrange — docs section 17: Coca-Cola at 42 kcal/100ml, 1 can = 350 ml -> 147 kcal.
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

        // Act
        var result = NutritionCalculator.Calculate(cocaCola, canServing.Id, servingCount: 1);

        // Assert
        result.Calories.Value.Should().Be(147);
    }

    [Fact]
    public void Calculate_ByServing_ScalesWithServingCount()
    {
        // Arrange
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

        // Act
        var result = NutritionCalculator.Calculate(cocaCola, canServing.Id, servingCount: 2);

        // Assert
        result.Calories.Value.Should().Be(294);
    }

    [Fact]
    public void Calculate_ByServing_Throws_WhenServingDoesNotBelongToFood()
    {
        // Arrange
        var cocaCola = Food.Create(
            FoodId.NewId(),
            "Coca-Cola",
            FoodSource.Usda,
            "67890",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            new Kilocalories(42),
            new Grams(0),
            new Grams(10.6),
            new Grams(0));

        // Act
        var act = () => NutritionCalculator.Calculate(cocaCola, FoodServingId.NewId(), servingCount: 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calculate_ByServing_Throws_WhenServingCountIsNotPositive()
    {
        // Arrange
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

        // Act
        var act = () => NutritionCalculator.Calculate(cocaCola, canServing.Id, servingCount: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
