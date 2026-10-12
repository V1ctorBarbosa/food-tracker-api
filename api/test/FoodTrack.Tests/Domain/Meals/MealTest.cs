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
        var loggedAt = new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero);
        var items = new[] { CreateItem(260, 5.4, 56, 0.6) };

        // Act
        var meal = Meal.Create(id, userId, loggedAt, MealType.Lunch, items);

        // Assert
        meal.Id.Should().Be(id);
        meal.UserId.Should().Be(userId);
        meal.LoggedAt.Should().Be(loggedAt);
        meal.MealType.Should().Be(MealType.Lunch);
        meal.Items.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Create_Throws_WhenItemsIsEmpty()
    {
        // Act
        var act = () => Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, []);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_WhenItemsExceedTheMaximumItemCount()
    {
        // Arrange
        var items = Enumerable.Range(0, Meal.MaximumItemCount + 1).Select(_ => CreateItem(100, 1, 1, 1)).ToList();

        // Act
        var act = () => Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, items);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(Meal.MaximumItemCount)]
    public void EnsureItemCountIsAllowed_DoesNotThrow_WhenTheCountIsWithinTheLimit(int itemCount)
    {
        // Act
        var act = () => Meal.EnsureItemCountIsAllowed(itemCount);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Meal.MaximumItemCount + 1)]
    public void EnsureItemCountIsAllowed_Throws_WhenTheCountIsOutsideTheLimit(int itemCount)
    {
        // Act
        var act = () => Meal.EnsureItemCountIsAllowed(itemCount);

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
        var meal = Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, items);

        // Assert
        meal.TotalCalories.Value.Should().Be(640);
        meal.TotalProtein.Value.Should().Be(60.3);
        meal.TotalCarbs.Value.Should().Be(79.7);
        meal.TotalFat.Value.Should().BeApproximately(6.1, 0.0001);
    }

    [Fact]
    public void ToSummary_CarriesTheMealIdentityAndItsDerivedTotals_WithoutTheItems()
    {
        // Arrange
        var id = MealId.NewId();
        var loggedAt = new DateTimeOffset(2026, 9, 3, 12, 40, 0, TimeSpan.Zero);
        var items = new[] { CreateItem(260, 5.4, 56, 0.6), CreateItem(248, 46, 0, 5) };
        var meal = Meal.Create(id, new UserId("firebase-uid-123"), loggedAt, MealType.Dinner, items);

        // Act
        var summary = meal.ToSummary();

        // Assert
        summary.Id.Should().Be(id);
        summary.LoggedAt.Should().Be(loggedAt);
        summary.MealType.Should().Be(MealType.Dinner);
        summary.TotalCalories.Should().Be(meal.TotalCalories);
        summary.TotalProtein.Should().Be(meal.TotalProtein);
        summary.TotalCarbs.Should().Be(meal.TotalCarbs);
        summary.TotalFat.Should().Be(meal.TotalFat);
    }

    [Fact]
    public void Create_HasNoCaptures_WhenNoneAreGiven()
    {
        // Act
        var meal = Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, [CreateItem(100, 1, 1, 1)]);

        // Assert
        meal.Captures.Should().BeEmpty();
    }

    [Fact]
    public void Create_ReturnsMeal_MixingCatalogAndEstimatedItems_FromItsCapture()
    {
        // Arrange
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, description: null);
        var items = new[] { CreateItem(260, 5.4, 56, 0.6), CreateEstimatedItem(capture.Id) };

        // Act
        var meal = Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, items, [capture]);

        // Assert
        meal.Items.Should().HaveCount(2);
        meal.Captures.Should().ContainSingle().Which.Should().Be(capture);
        meal.TotalCalories.Value.Should().Be(260 + 248);
    }

    [Fact]
    public void Create_KeepsACapture_WhoseItemsWereAllRemoved()
    {
        // Arrange
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, description: null);

        // Act
        var meal = Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, [CreateItem(100, 1, 1, 1)], [capture]);

        // Assert
        meal.Captures.Should().ContainSingle();
    }

    [Fact]
    public void Create_Throws_WhenAnEstimatedItemComesFromACaptureTheMealDoesNotHave()
    {
        // Arrange
        var items = new[] { CreateEstimatedItem(CaptureId.NewId()) };

        // Act
        var act = () => Meal.Create(MealId.NewId(), new UserId("firebase-uid-123"), DateTimeOffset.UtcNow, MealType.Lunch, items, []);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_WhenTheSameCaptureAppearsTwice()
    {
        // Arrange
        var capture = MealCapture.Create(CaptureId.NewId(), hasPhoto: true, description: null);

        // Act
        var act = () => Meal.Create(
            MealId.NewId(),
            new UserId("firebase-uid-123"),
            DateTimeOffset.UtcNow,
            MealType.Lunch,
            [CreateItem(100, 1, 1, 1)],
            [capture, capture]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(Meal.MaximumCaptureCount, false)]
    [InlineData(Meal.MaximumCaptureCount + 1, true)]
    public void Create_EnforcesTheCaptureLimit(int captureCount, bool shouldThrow)
    {
        // Arrange
        var captures = Enumerable.Range(0, captureCount)
            .Select(_ => MealCapture.Create(CaptureId.NewId(), hasPhoto: true, description: null))
            .ToList();

        // Act
        var act = () => Meal.Create(
            MealId.NewId(),
            new UserId("firebase-uid-123"),
            DateTimeOffset.UtcNow,
            MealType.Lunch,
            [CreateItem(100, 1, 1, 1)],
            captures);

        // Assert
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
            return;
        }

        act.Should().NotThrow();
    }

    private static MealItem CreateEstimatedItem(CaptureId captureId) => MealItem.FromEstimatedFood(
        MealItemId.NewId(),
        EstimatedFood.Create(
            "Arroz integral cozido",
            MeasurementUnit.Gram,
            new NutritionAmount(new Kilocalories(124), new Grams(2.6), new Grams(25.8), new Grams(1))),
        captureId,
        200,
        MeasurementUnit.Gram);

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
