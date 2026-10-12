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

    [Theory]
    [InlineData(MeasurementUnit.Gram, MeasurementUnit.Milliliter)]
    [InlineData(MeasurementUnit.Gram, MeasurementUnit.Liter)]
    [InlineData(MeasurementUnit.Milliliter, MeasurementUnit.Gram)]
    [InlineData(MeasurementUnit.Milliliter, MeasurementUnit.Kilogram)]
    public void Calculate_ThrowsArgumentException_WhenTheUnitMeasuresSomethingElseThanTheFood(
        MeasurementUnit foodUnit,
        MeasurementUnit loggedUnit)
    {
        // Arrange
        var food = Food.Create(
            FoodId.NewId(),
            "Rice",
            FoodSource.Taco,
            "12345",
            new BaseUnit(100, foodUnit),
            new Kilocalories(130),
            new Grams(2.7),
            new Grams(28),
            new Grams(0.3));

        // Act
        var act = () => NutritionCalculator.Calculate(food, 200, loggedUnit);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Rice*");
    }

    [Fact]
    public void Calculate_ScalesADrinkByMilliliters_ForACanOfCocaCola()
    {
        // Arrange: regular Coca-Cola, 42 kcal and 10.5 g carbs per 100 ml.
        var cocaCola = Food.Create(
            FoodId.NewId(),
            "Coca-Cola",
            FoodSource.OpenFoodFacts,
            "7894900011708",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            new Kilocalories(42),
            new Grams(0),
            new Grams(10.5),
            new Grams(0));

        // Act
        var result = NutritionCalculator.Calculate(cocaCola, 350, MeasurementUnit.Milliliter);

        // Assert
        result.Calories.Value.Should().Be(147);
        result.Carbs.Value.Should().BeApproximately(36.75, 0.001);
    }

    [Fact]
    public void Calculate_ScalesNutritionPerBaseUnit_WithoutAFood()
    {
        // Arrange
        var nutritionPer100Grams = new NutritionAmount(new Kilocalories(130), new Grams(2.7), new Grams(28), new Grams(0.3));

        // Act
        var result = NutritionCalculator.Calculate(
            "Arroz branco cozido",
            new BaseUnit(100, MeasurementUnit.Gram),
            nutritionPer100Grams,
            0.15,
            MeasurementUnit.Kilogram);

        // Assert
        result.Calories.Value.Should().Be(195);
        result.Protein.Value.Should().BeApproximately(4.05, 0.0001);
        result.Carbs.Value.Should().BeApproximately(42, 0.0001);
        result.Fat.Value.Should().BeApproximately(0.45, 0.0001);
    }

    [Fact]
    public void Calculate_NamesWhatIsMeasured_WhenTheUnitMeasuresSomethingElseWithoutAFood()
    {
        // Arrange
        var nutritionPer100Milliliters = new NutritionAmount(new Kilocalories(45), new Grams(0.7), new Grams(10.4), new Grams(0.2));

        // Act
        var act = () => NutritionCalculator.Calculate(
            "Suco de laranja",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            nutritionPer100Milliliters,
            300,
            MeasurementUnit.Gram);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Suco de laranja*");
    }
}
