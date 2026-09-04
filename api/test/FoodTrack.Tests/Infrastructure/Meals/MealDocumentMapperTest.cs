using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using FoodTrack.Infrastructure.Meals;

namespace FoodTrack.Tests.Infrastructure.Meals;

public sealed class MealDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromMeal()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var meal = Meal.Create(
            MealId.NewId(),
            userId,
            new DateOnly(2026, 9, 3),
            MealType.Lunch,
            [CreateItem()]);

        // Act
        var document = MealDocumentMapper.ToDocument(meal);

        // Assert
        document.UserId.Should().Be(userId.Value);
        document.Date.Should().Be(new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));
        document.MealType.Should().Be(nameof(MealType.Lunch));
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument_IncludingItems()
    {
        // Arrange
        var id = MealId.NewId();
        var userId = new UserId("firebase-uid-123");
        var items = new[] { CreateItem() };
        var originalMeal = Meal.Create(id, userId, new DateOnly(2026, 9, 3), MealType.Breakfast, items);

        // Act
        var document = MealDocumentMapper.ToDocument(originalMeal);
        var roundTrippedMeal = MealDocumentMapper.ToDomain(id, userId, document, items);

        // Assert
        roundTrippedMeal.Id.Should().Be(id);
        roundTrippedMeal.UserId.Should().Be(userId);
        roundTrippedMeal.Date.Should().Be(originalMeal.Date);
        roundTrippedMeal.MealType.Should().Be(originalMeal.MealType);
        roundTrippedMeal.Items.Should().BeEquivalentTo(items);
    }

    private static MealItem CreateItem() => MealItem.Create(
        MealItemId.NewId(),
        FoodId.NewId(),
        200,
        MeasurementUnit.Gram,
        new Kilocalories(260),
        new Grams(5.4),
        new Grams(56),
        new Grams(0.6));
}
