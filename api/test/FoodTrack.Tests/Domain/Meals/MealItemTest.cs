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
        item.FoodId.Should().Be(foodId);
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
}
