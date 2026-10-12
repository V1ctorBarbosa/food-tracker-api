using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Infrastructure.Meals;

namespace FoodTrack.Tests.Infrastructure.Meals;

public sealed class MealItemDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromACatalogItem()
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
        var document = MealItemDocumentMapper.ToDocument(item);

        // Assert
        document.Origin.Should().Be(MealItemOriginNames.Catalog);
        document.FoodId.Should().Be(foodId.Value);
        document.Quantity.Should().Be(200);
        document.Unit.Should().Be(nameof(MeasurementUnit.Gram));
        document.Calories.Should().Be(260);
        document.Protein.Should().Be(5.4);
        document.Carbs.Should().Be(56);
        document.Fat.Should().Be(0.6);
        document.Name.Should().BeNull();
        document.CaptureId.Should().BeNull();
    }

    [Fact]
    public void ToDocument_StoresTheEstimatedFood_OnAnEstimatedItem()
    {
        // Arrange
        var captureId = CaptureId.NewId();
        var item = CreateEstimatedItem(captureId);

        // Act
        var document = MealItemDocumentMapper.ToDocument(item);

        // Assert
        document.Origin.Should().Be(MealItemOriginNames.Estimated);
        document.FoodId.Should().BeNull();
        document.Name.Should().Be("Suco de laranja");
        document.CaptureId.Should().Be(captureId.Value);
        document.BaseUnit.Should().Be(nameof(MeasurementUnit.Milliliter));
        document.CaloriesPerBaseUnit.Should().Be(45);
        document.ProteinPerBaseUnit.Should().Be(0.7);
        document.CarbsPerBaseUnit.Should().Be(10.4);
        document.FatPerBaseUnit.Should().Be(0.2);
        document.Quantity.Should().Be(300);
        document.Calories.Should().Be(item.Calories.Value);
    }

    [Fact]
    public void ToDomain_RoundTrips_ACatalogItem_ThroughToDocument()
    {
        // Arrange
        var id = MealItemId.NewId();
        var originalItem = MealItem.Create(
            id,
            FoodId.NewId(),
            200,
            MeasurementUnit.Gram,
            new Kilocalories(260),
            new Grams(5.4),
            new Grams(56),
            new Grams(0.6));

        // Act
        var document = MealItemDocumentMapper.ToDocument(originalItem);
        var roundTrippedItem = MealItemDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedItem.Should().BeEquivalentTo(originalItem);
    }

    [Fact]
    public void ToDomain_RoundTrips_AnEstimatedItem_ThroughToDocument()
    {
        // Arrange
        var originalItem = CreateEstimatedItem(CaptureId.NewId());

        // Act
        var document = MealItemDocumentMapper.ToDocument(originalItem);
        var roundTrippedItem = MealItemDocumentMapper.ToDomain(originalItem.Id, document);

        // Assert
        roundTrippedItem.Should().BeEquivalentTo(originalItem);
        roundTrippedItem.Origin.Should().Be(originalItem.Origin);
    }

    [Fact]
    public void ToDomain_ReadsACatalogItem_WhenTheDocumentPredatesTheOrigin()
    {
        // Arrange
        var foodId = FoodId.NewId();
        var legacyDocument = new MealItemDocument
        {
            FoodId = foodId.Value,
            Quantity = 200,
            Unit = nameof(MeasurementUnit.Gram),
            Calories = 260,
            Protein = 5.4,
            Carbs = 56,
            Fat = 0.6,
        };

        // Act
        var item = MealItemDocumentMapper.ToDomain(MealItemId.NewId(), legacyDocument);

        // Assert
        item.Origin.Should().Be(new CatalogFoodOrigin(foodId));
    }

    [Fact]
    public void ToDomain_Throws_WhenAnEstimatedDocumentIsMissingItsFood()
    {
        // Arrange
        var document = MealItemDocumentMapper.ToDocument(CreateEstimatedItem(CaptureId.NewId()));
        document.Name = null;

        // Act
        var act = () => MealItemDocumentMapper.ToDomain(MealItemId.NewId(), document);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Name*");
    }

    [Theory]
    [InlineData("Gram", MeasurementUnitCategory.Volume, "Milliliter")]
    [InlineData("Kilogram", MeasurementUnitCategory.Volume, "Liter")]
    [InlineData("Milliliter", MeasurementUnitCategory.Mass, "Gram")]
    [InlineData("Milliliter", MeasurementUnitCategory.Volume, "Milliliter")]
    public void ReclassifyUnit_ReturnsTheStoredUnitNameInTheTargetCategory(
        string storedUnit,
        MeasurementUnitCategory category,
        string expectedUnit)
    {
        // Act
        var reclassifiedUnit = MealItemDocumentMapper.ReclassifyUnit(storedUnit, category);

        // Assert
        reclassifiedUnit.Should().Be(expectedUnit);
    }

    private static MealItem CreateEstimatedItem(CaptureId captureId) => MealItem.FromEstimatedFood(
        MealItemId.NewId(),
        EstimatedFood.Create(
            "Suco de laranja",
            MeasurementUnit.Milliliter,
            new NutritionAmount(new Kilocalories(45), new Grams(0.7), new Grams(10.4), new Grams(0.2))),
        captureId,
        300,
        MeasurementUnit.Milliliter);
}
