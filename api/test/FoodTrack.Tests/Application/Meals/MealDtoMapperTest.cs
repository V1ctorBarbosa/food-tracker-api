using FluentAssertions;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Tests.Application.Meals;

public sealed class MealDtoMapperTest
{
    [Fact]
    public void ToDto_SetsTheFoodIdOnly_ForACatalogItem()
    {
        // Arrange
        var foodId = FoodId.NewId();
        var item = MealItem.Create(
            MealItemId.NewId(),
            foodId,
            200,
            MeasurementUnit.Gram,
            new Kilocalories(260),
            new Grams(5.4),
            new Grams(56),
            new Grams(0.6));

        // Act
        var dto = MealDtoMapper.ToDto(item);

        // Assert
        dto.FoodId.Should().Be(foodId.Value);
        dto.EstimatedFood.Should().BeNull();
        dto.Quantity.Should().Be(200);
        dto.Unit.Should().Be(nameof(MeasurementUnit.Gram));
        dto.Calories.Should().Be(260);
    }

    [Fact]
    public void ToDto_SetsTheEstimatedFoodOnly_ForAnEstimatedItem()
    {
        // Arrange
        var captureId = CaptureId.NewId();
        var item = MealItem.FromEstimatedFood(
            MealItemId.NewId(),
            EstimatedFood.Create(
                "Peito de frango grelhado",
                MeasurementUnit.Gram,
                new NutritionAmount(new Kilocalories(160), new Grams(32), new Grams(0), new Grams(3))),
            captureId,
            150,
            MeasurementUnit.Gram);

        // Act
        var dto = MealDtoMapper.ToDto(item);

        // Assert
        dto.FoodId.Should().BeNull();
        dto.EstimatedFood.Should().Be(new EstimatedFoodDto(
            "Peito de frango grelhado",
            captureId.Value,
            nameof(MeasurementUnit.Gram),
            160,
            32,
            0,
            3));
        dto.Calories.Should().Be(240);
    }

    [Fact]
    public void ToDto_IncludesTheMealsCaptures()
    {
        // Arrange
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, new MealDescription("o frango é empanado"));
        var meal = Meal.Create(
            MealId.NewId(),
            new UserId("firebase-uid-123"),
            DateTimeOffset.UtcNow,
            MealType.Lunch,
            [MealItem.Create(MealItemId.NewId(), FoodId.NewId(), 100, MeasurementUnit.Gram, new Kilocalories(100), new Grams(1), new Grams(1), new Grams(1))],
            [capture]);

        // Act
        var dto = MealDtoMapper.ToDto(meal);

        // Assert
        dto.Captures.Should().Equal(new MealCaptureDto(capture.Id.Value, true, "o frango é empanado"));
    }
}
