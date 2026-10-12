using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;

namespace FoodTrack.Tests.Domain.Foods;

public sealed class FoodTest
{
    [Fact]
    public void Create_ReturnsFood_WithGivenValues()
    {
        // Arrange
        var id = FoodId.NewId();
        var baseUnit = new BaseUnit(100, MeasurementUnit.Gram);

        // Act
        var food = Food.Create(
            id,
            "Chicken breast",
            FoodSource.Usda,
            "171077",
            baseUnit,
            new Kilocalories(165),
            new Grams(31),
            new Grams(0),
            new Grams(3.6));

        // Assert
        food.Id.Should().Be(id);
        food.Name.Should().Be("Chicken breast");
        food.Source.Should().Be(FoodSource.Usda);
        food.ExternalId.Should().Be("171077");
        food.BaseUnit.Should().Be(baseUnit);
        food.CaloriesPerBaseUnit.Value.Should().Be(165);
        food.ProteinPerBaseUnit.Value.Should().Be(31);
        food.CarbsPerBaseUnit.Value.Should().Be(0);
        food.FatPerBaseUnit.Value.Should().Be(3.6);
        food.Servings.Should().BeEmpty();
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var food = CreateValidFood(name: "  Chicken breast  ");

        // Assert
        food.Name.Should().Be("Chicken breast");
    }

    [Fact]
    public void Create_AllowsNullExternalId_ForCustomFoods()
    {
        // Act
        var food = Food.Create(
            FoodId.NewId(),
            "My whey",
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(30, MeasurementUnit.Gram),
            new Kilocalories(120),
            new Grams(24),
            new Grams(3),
            new Grams(1.5));

        // Assert
        food.ExternalId.Should().BeNull();
        food.Source.Should().Be(FoodSource.Custom);
    }

    [Fact]
    public void Create_SetsOwnerId_WhenProvided()
    {
        // Arrange
        var ownerId = new UserId("firebase-uid-123");

        // Act
        var food = Food.Create(
            FoodId.NewId(),
            "My whey",
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(120),
            new Grams(24),
            new Grams(3),
            new Grams(1.5),
            ownerId: ownerId);

        // Assert
        food.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public void Create_LeavesOwnerIdNull_ByDefault()
    {
        // Act
        var food = CreateValidFood();

        // Assert
        food.OwnerId.Should().BeNull();
    }

    [Fact]
    public void Create_KeepsGivenServings()
    {
        // Arrange
        var serving = FoodServing.Create(FoodServingId.NewId(), "can", 350, MeasurementUnit.Milliliter);

        // Act
        var food = Food.Create(
            FoodId.NewId(),
            "Coca-Cola",
            FoodSource.Usda,
            "12345",
            new BaseUnit(100, MeasurementUnit.Milliliter),
            new Kilocalories(42),
            new Grams(0),
            new Grams(10.6),
            new Grams(0),
            [serving]);

        // Assert
        food.Servings.Should().ContainSingle().Which.Should().Be(serving);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Act
        var act = () => CreateValidFood(name: name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsVisibleTo_ReturnsTrue_ForASharedFood_WhoeverAsks()
    {
        // Arrange
        var sharedFood = CreateValidFood();

        // Act
        var isVisible = sharedFood.IsVisibleTo(new UserId("any-user"));

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    public void IsVisibleTo_ReturnsTrue_OnlyForTheOwner_OfACustomFood()
    {
        // Arrange
        var ownerId = new UserId("owner-uid");
        var customFood = Food.Create(
            FoodId.NewId(),
            "My whey",
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(120),
            new Grams(24),
            new Grams(3),
            new Grams(1.5),
            ownerId: ownerId);

        // Act
        var isVisibleToOwner = customFood.IsVisibleTo(ownerId);
        var isVisibleToSomeoneElse = customFood.IsVisibleTo(new UserId("someone-else"));

        // Assert
        isVisibleToOwner.Should().BeTrue();
        isVisibleToSomeoneElse.Should().BeFalse();
    }

    [Fact]
    public void MeasuredIn_MovesTheBaseUnit_KeepingEverythingElse()
    {
        // Arrange
        var ownerId = new UserId("owner-uid");
        var serving = FoodServing.Create(FoodServingId.NewId(), "glass", 200, MeasurementUnit.Gram);
        var food = Food.Create(
            FoodId.NewId(),
            "Orange juice",
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(45),
            new Grams(0.7),
            new Grams(10.4),
            new Grams(0.2),
            [serving],
            ownerId);

        // Act
        var measuredInMilliliters = food.MeasuredIn(MeasurementUnit.Milliliter);

        // Assert
        measuredInMilliliters.BaseUnit.Should().Be(new BaseUnit(100, MeasurementUnit.Milliliter));
        measuredInMilliliters.Should().BeEquivalentTo(food, options => options.Excluding(candidate => candidate.BaseUnit));
    }

    private static Food CreateValidFood(string name = "Chicken breast") => Food.Create(
        FoodId.NewId(),
        name,
        FoodSource.Usda,
        "171077",
        new BaseUnit(100, MeasurementUnit.Gram),
        new Kilocalories(165),
        new Grams(31),
        new Grams(0),
        new Grams(3.6));
}
