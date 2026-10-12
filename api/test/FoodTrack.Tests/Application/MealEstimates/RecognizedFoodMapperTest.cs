using FluentAssertions;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Application.MealEstimates;

public sealed class RecognizedFoodMapperTest
{
    private static readonly RecognizedFood GrilledChicken = new("Peito de frango grelhado", 150, "Gram", 159.6, 32, 0, 2.5);

    [Fact]
    public void TryToItem_ReturnsAnEstimatedItem_FromTheCapture()
    {
        // Arrange
        var captureId = CaptureId.NewId();

        // Act
        var item = RecognizedFoodMapper.TryToItem(GrilledChicken, captureId);

        // Assert
        item.Should().NotBeNull();
        var origin = item!.Origin.Should().BeOfType<EstimatedFoodOrigin>().Subject;
        origin.CaptureId.Should().Be(captureId);
        origin.Food.Name.Should().Be("Peito de frango grelhado");
        origin.Food.NutritionPerBaseUnit.Calories.Value.Should().Be(160);
        item.Quantity.Should().Be(150);
        item.Unit.Should().Be(MeasurementUnit.Gram);
        item.Calories.Value.Should().Be(240);
    }

    [Fact]
    public void TryToItem_MeasuresADrinkInMilliliters()
    {
        // Arrange
        var orangeJuice = new RecognizedFood("Suco de laranja", 300, "Milliliter", 45, 0.7, 10.4, 0.2);

        // Act
        var item = RecognizedFoodMapper.TryToItem(orangeJuice, CaptureId.NewId());

        // Assert
        item!.Unit.Should().Be(MeasurementUnit.Milliliter);
    }

    [Theory]
    [InlineData("", 150, "Gram", 160)]
    [InlineData("Frango", 0, "Gram", 160)]
    [InlineData("Frango", MealItem.MaximumEstimatedQuantity + 1, "Gram", 160)]
    [InlineData("Frango", 150, "cup", 160)]
    [InlineData("Frango", 150, "Kilogram", 160)]
    [InlineData("Frango", 150, "Gram", 950)]
    [InlineData("Frango", 150, "Gram", -1)]
    public void TryToItem_ReturnsNull_WhenAValueIsImpossible(string name, double quantity, string baseUnit, double caloriesPer100)
    {
        // Arrange
        var recognizedFood = new RecognizedFood(name, quantity, baseUnit, caloriesPer100, 32, 0, 2.5);

        // Act
        var item = RecognizedFoodMapper.TryToItem(recognizedFood, CaptureId.NewId());

        // Assert
        item.Should().BeNull();
    }

    [Fact]
    public void ToValidItems_DropsOnlyTheImpossibleFoods()
    {
        // Arrange
        var impossibleFood = GrilledChicken with { CaloriesPerBaseUnit = 5_000 };

        // Act
        var items = RecognizedFoodMapper.ToValidItems([GrilledChicken, impossibleFood], CaptureId.NewId());

        // Assert
        items.Should().ContainSingle();
    }

    [Fact]
    public void ToValidItems_KeepsAtMostTheMealItemLimit()
    {
        // Arrange
        var recognizedFoods = Enumerable.Repeat(GrilledChicken, Meal.MaximumItemCount + 5).ToList();

        // Act
        var items = RecognizedFoodMapper.ToValidItems(recognizedFoods, CaptureId.NewId());

        // Assert
        items.Should().HaveCount(Meal.MaximumItemCount);
    }
}
