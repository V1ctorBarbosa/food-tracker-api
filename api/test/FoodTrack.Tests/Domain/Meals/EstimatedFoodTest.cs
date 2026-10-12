using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class EstimatedFoodTest
{
    private static readonly NutritionAmount BrownRicePer100Grams = new(new Kilocalories(124), new Grams(2.6), new Grams(25.8), new Grams(1));

    [Fact]
    public void Create_ReturnsEstimatedFood_PerOneHundredOfTheBaseUnit()
    {
        // Act
        var estimatedFood = EstimatedFood.Create("  Arroz integral cozido  ", MeasurementUnit.Gram, BrownRicePer100Grams);

        // Assert
        estimatedFood.Name.Should().Be("Arroz integral cozido");
        estimatedFood.BaseUnit.Should().Be(new BaseUnit(EstimatedFood.BaseQuantity, MeasurementUnit.Gram));
        estimatedFood.NutritionPerBaseUnit.Should().Be(BrownRicePer100Grams);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenTheNameIsEmpty(string? name)
    {
        // Act
        var act = () => EstimatedFood.Create(name!, MeasurementUnit.Gram, BrownRicePer100Grams);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_WhenTheNameIsLongerThanTheMaximum()
    {
        // Arrange
        var name = new string('a', EstimatedFood.MaximumNameLength + 1);

        // Act
        var act = () => EstimatedFood.Create(name, MeasurementUnit.Gram, BrownRicePer100Grams);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(MeasurementUnit.Kilogram)]
    [InlineData(MeasurementUnit.Liter)]
    public void Create_Throws_WhenTheBaseUnitIsNotGramOrMilliliter(MeasurementUnit baseUnit)
    {
        // Act
        var act = () => EstimatedFood.Create("Arroz", baseUnit, BrownRicePer100Grams);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_AcceptsPureFat()
    {
        // Arrange
        var oliveOil = new NutritionAmount(new Kilocalories(900), new Grams(0), new Grams(0), new Grams(100));

        // Act
        var act = () => EstimatedFood.Create("Azeite", MeasurementUnit.Gram, oliveOil);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_Throws_WhenCaloriesAreAboveWhatAnyFoodHas()
    {
        // Arrange
        var impossibleNutrition = new NutritionAmount(new Kilocalories(901), new Grams(0), new Grams(0), new Grams(100));

        // Act
        var act = () => EstimatedFood.Create("Azeite", MeasurementUnit.Gram, impossibleNutrition);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_WhenMacronutrientsWeighMoreThanTheBaseQuantity()
    {
        // Arrange
        var impossibleNutrition = new NutritionAmount(new Kilocalories(400), new Grams(40), new Grams(40), new Grams(21));

        // Act
        var act = () => EstimatedFood.Create("Granola", MeasurementUnit.Gram, impossibleNutrition);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateFor_ScalesTheNutritionToTheQuantity()
    {
        // Arrange
        var estimatedFood = EstimatedFood.Create("Arroz integral cozido", MeasurementUnit.Gram, BrownRicePer100Grams);

        // Act
        var nutrition = estimatedFood.CalculateFor(200, MeasurementUnit.Gram);

        // Assert
        nutrition.Calories.Value.Should().Be(248);
        nutrition.Protein.Value.Should().BeApproximately(5.2, 0.0001);
        nutrition.Carbs.Value.Should().BeApproximately(51.6, 0.0001);
        nutrition.Fat.Value.Should().BeApproximately(2, 0.0001);
    }

    [Fact]
    public void CalculateFor_Throws_WhenTheUnitMeasuresSomethingElse()
    {
        // Arrange
        var orangeJuice = EstimatedFood.Create(
            "Suco de laranja",
            MeasurementUnit.Milliliter,
            new NutritionAmount(new Kilocalories(45), new Grams(0.7), new Grams(10.4), new Grams(0.2)));

        // Act
        var act = () => orangeJuice.CalculateFor(300, MeasurementUnit.Gram);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Suco de laranja*");
    }
}
