using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class MealItemTest
{
    [Fact]
    public void Create_ReturnsMealItem_WithGivenValues()
    {
        // Arrange
        var id = MealItemId.NewId();
        var foodId = FoodId.NewId();

        // Act
        var item = MealItem.Create(
            id,
            foodId,
            200,
            MeasurementUnit.Gram,
            new Kilocalories(260),
            new Grams(5.4),
            new Grams(56),
            new Grams(0.6));

        // Assert
        item.Id.Should().Be(id);
        item.Origin.Should().Be(new CatalogFoodOrigin(foodId));
        item.Quantity.Should().Be(200);
        item.Unit.Should().Be(MeasurementUnit.Gram);
        item.Calories.Value.Should().Be(260);
        item.Protein.Value.Should().Be(5.4);
        item.Carbs.Value.Should().Be(56);
        item.Fat.Value.Should().Be(0.6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Throws_WhenQuantityIsNotPositive(double quantity)
    {
        // Act
        var act = () => MealItem.Create(
            MealItemId.NewId(),
            FoodId.NewId(),
            quantity,
            MeasurementUnit.Gram,
            new Kilocalories(260),
            new Grams(5.4),
            new Grams(56),
            new Grams(0.6));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromCatalogFood_TakesTheSnapshotFromTheFood()
    {
        // Arrange
        var rice = Food.Create(
            FoodId.NewId(),
            "Rice",
            FoodSource.Taco,
            "1",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(130),
            new Grams(2.7),
            new Grams(28),
            new Grams(0.3));

        // Act
        var item = MealItem.FromCatalogFood(MealItemId.NewId(), rice, 200, MeasurementUnit.Gram);

        // Assert
        item.Origin.Should().Be(new CatalogFoodOrigin(rice.Id));
        item.Quantity.Should().Be(200);
        item.Calories.Value.Should().Be(260);
        item.Protein.Value.Should().BeApproximately(5.4, 0.0001);
        item.Carbs.Value.Should().BeApproximately(56, 0.0001);
        item.Fat.Value.Should().BeApproximately(0.6, 0.0001);
    }

    [Fact]
    public void FromEstimatedFood_TakesTheSnapshotFromTheValuesPerBaseUnit()
    {
        // Arrange
        var captureId = CaptureId.NewId();
        var chicken = CreateGrilledChicken();

        // Act
        var item = MealItem.FromEstimatedFood(MealItemId.NewId(), chicken, captureId, 150, MeasurementUnit.Gram);

        // Assert
        item.Origin.Should().Be(new EstimatedFoodOrigin(chicken, captureId));
        item.Quantity.Should().Be(150);
        item.Unit.Should().Be(MeasurementUnit.Gram);
        item.Calories.Value.Should().Be(240);
        item.Protein.Value.Should().BeApproximately(48, 0.0001);
        item.Fat.Value.Should().BeApproximately(4.5, 0.0001);
    }

    [Fact]
    public void FromEstimatedFood_DoublesTheSnapshot_WhenTheQuantityDoubles()
    {
        // Arrange
        var chicken = CreateGrilledChicken();
        var captureId = CaptureId.NewId();

        // Act
        var smallerItem = MealItem.FromEstimatedFood(MealItemId.NewId(), chicken, captureId, 150, MeasurementUnit.Gram);
        var largerItem = MealItem.FromEstimatedFood(MealItemId.NewId(), chicken, captureId, 300, MeasurementUnit.Gram);

        // Assert
        largerItem.Calories.Value.Should().Be(smallerItem.Calories.Value * 2);
        largerItem.Protein.Value.Should().BeApproximately(smallerItem.Protein.Value * 2, 0.0001);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(MealItem.MaximumEstimatedQuantity + 1)]
    public void FromEstimatedFood_Throws_WhenTheQuantityIsOutsideTheLimit(double quantity)
    {
        // Act
        var act = () => MealItem.FromEstimatedFood(MealItemId.NewId(), CreateGrilledChicken(), CaptureId.NewId(), quantity, MeasurementUnit.Gram);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromEstimatedFood_AcceptsTheMaximumQuantity()
    {
        // Act
        var act = () => MealItem.FromEstimatedFood(
            MealItemId.NewId(),
            CreateGrilledChicken(),
            CaptureId.NewId(),
            MealItem.MaximumEstimatedQuantity,
            MeasurementUnit.Gram);

        // Assert
        act.Should().NotThrow();
    }

    private static EstimatedFood CreateGrilledChicken() => EstimatedFood.Create(
        "Peito de frango grelhado",
        MeasurementUnit.Gram,
        new NutritionAmount(new Kilocalories(160), new Grams(32), new Grams(0), new Grams(3)));
}
