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
        var loggedAt = new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero);
        var meal = Meal.Create(
            MealId.NewId(),
            userId,
            loggedAt,
            MealType.Lunch,
            [CreateItem()]);

        // Act
        var document = MealDocumentMapper.ToDocument(meal);

        // Assert
        document.UserId.Should().Be(userId.Value);
        document.LoggedAt.Should().Be(loggedAt);
        document.MealType.Should().Be(nameof(MealType.Lunch));
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument_IncludingItems()
    {
        // Arrange
        var id = MealId.NewId();
        var userId = new UserId("firebase-uid-123");
        var items = new[] { CreateItem() };
        var loggedAt = new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero);
        var originalMeal = Meal.Create(id, userId, loggedAt, MealType.Breakfast, items);

        // Act
        var document = MealDocumentMapper.ToDocument(originalMeal);
        var roundTrippedMeal = MealDocumentMapper.ToDomain(id, userId, document, items);

        // Assert
        roundTrippedMeal.Id.Should().Be(id);
        roundTrippedMeal.UserId.Should().Be(userId);
        roundTrippedMeal.LoggedAt.Should().Be(originalMeal.LoggedAt);
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
