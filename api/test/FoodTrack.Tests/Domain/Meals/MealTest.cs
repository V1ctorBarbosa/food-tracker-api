using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Tests.Domain.Meals;

public sealed class MealTest
{
    [Fact]
    public void Create_ReturnsMeal_WithGivenValues()
    {
        // Arrange
        var id = MealId.NewId();
        var userId = new UserId("firebase-uid-123");
        var date = new DateOnly(2026, 9, 3);
        var items = new[] { CreateItem(260, 5.4, 56, 0.6) };

        // Act
        var meal = Meal.Create(id, userId, date, MealType.Lunch, items);

        // Assert
        meal.Id.Should().Be(id);
        meal.UserId.Should().Be(userId);
        meal.Date.Should().Be(date);
        meal.MealType.Should().Be(MealType.Lunch);
        meal.Items.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Create_Throws_WhenItemsIsEmpty()
    {
        // Act
        var act = () => Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), new DateOnly(2026, 9, 3), MealType.Lunch, []);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Totals_SumAcrossAllItems()
    {
        // Arrange — docs section 25: rice + chicken + beans summed into daily totals.
        var items = new[]
        {
            CreateItem(260, 5.4, 56, 0.6),
            CreateItem(248, 46, 0, 5),
            CreateItem(132, 8.9, 23.7, 0.5),
        };

        // Act
        var meal = Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), new DateOnly(2026, 9, 3), MealType.Lunch, items);

        // Assert
        meal.TotalCalories.Value.Should().Be(640);
        meal.TotalProtein.Value.Should().Be(60.3);
        meal.TotalCarbs.Value.Should().Be(79.7);
        meal.TotalFat.Value.Should().BeApproximately(6.1, 0.0001);
    }

    private static MealItem CreateItem(int calories, double protein, double carbs, double fat) => MealItem.Create(
        MealItemId.NewId(),
        FoodId.NewId(),
        200,
        MeasurementUnit.Gram,
        new Kilocalories(calories),
        new Grams(protein),
        new Grams(carbs),
        new Grams(fat));
}
