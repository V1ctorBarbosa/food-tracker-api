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

    [Fact]
    public void ToDocument_StoresTheTotalsDerivedFromTheItems()
    {
        // Arrange
        var meal = Meal.Create(
            MealId.NewId(),
            new UserId("firebase-uid-123"),
            new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero),
            MealType.Lunch,
            [CreateItem(), CreateItem()]);

        // Act
        var document = MealDocumentMapper.ToDocument(meal);

        // Assert
        document.TotalCalories.Should().Be(meal.TotalCalories.Value);
        document.TotalProtein.Should().Be(meal.TotalProtein.Value);
        document.TotalCarbs.Should().Be(meal.TotalCarbs.Value);
        document.TotalFat.Should().Be(meal.TotalFat.Value);
    }

    [Fact]
    public void TryToSummary_ReadsTheStoredTotals_WithoutNeedingItems()
    {
        // Arrange
        var id = MealId.NewId();
        var loggedAt = new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero);
        var document = new MealDocument
        {
            UserId = "firebase-uid-123",
            LoggedAt = loggedAt,
            MealType = nameof(MealType.Dinner),
            TotalCalories = 520,
            TotalProtein = 31.5,
            TotalCarbs = 60,
            TotalFat = 12.25,
        };

        // Act
        var summary = MealDocumentMapper.TryToSummary(id, document);

        // Assert
        summary.Should().NotBeNull();
        summary!.Id.Should().Be(id);
        summary.LoggedAt.Should().Be(loggedAt);
        summary.MealType.Should().Be(MealType.Dinner);
        summary.TotalCalories.Value.Should().Be(520);
        summary.TotalProtein.Value.Should().Be(31.5);
        summary.TotalCarbs.Value.Should().Be(60);
        summary.TotalFat.Value.Should().Be(12.25);
    }

    [Fact]
    public void TryToSummary_ReturnsNull_ForAMealSavedBeforeTotalsWereStored()
    {
        // Arrange
        var document = new MealDocument
        {
            UserId = "firebase-uid-123",
            LoggedAt = DateTimeOffset.UtcNow,
            MealType = nameof(MealType.Lunch),
        };

        // Act
        var summary = MealDocumentMapper.TryToSummary(MealId.NewId(), document);

        // Assert
        summary.Should().BeNull();
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void TryToSummary_ReturnsNull_WhenOnlyOneTotalIsMissing(bool missesCalories, bool missesProtein, bool missesCarbs, bool missesFat)
    {
        // Arrange
        var document = new MealDocument
        {
            UserId = "firebase-uid-123",
            LoggedAt = DateTimeOffset.UtcNow,
            MealType = nameof(MealType.Lunch),
            TotalCalories = ValueUnlessMissing(500, missesCalories),
            TotalProtein = ValueUnlessMissing(30, missesProtein),
            TotalCarbs = ValueUnlessMissing(50, missesCarbs),
            TotalFat = ValueUnlessMissing(10, missesFat),
        };

        // Act
        var summary = MealDocumentMapper.TryToSummary(MealId.NewId(), document);

        // Assert
        summary.Should().BeNull();
    }

    private static int? ValueUnlessMissing(int value, bool isMissing)
    {
        if (isMissing)
        {
            return null;
        }

        return value;
    }

    private static double? ValueUnlessMissing(double value, bool isMissing)
    {
        if (isMissing)
        {
            return null;
        }

        return value;
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
