using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Infrastructure.Meals;

namespace FoodTrack.Tests.Infrastructure.Meals;

public sealed class MealItemDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromMealItem()
    {
        // Arrange
        var item = MealItem.Create(
            MealItemId.NewId(),
            FoodId.NewId(),
            200,
            MeasurementUnit.Gram,
            new Kilocalories(260),
            new Grams(5.4),
            new Grams(56),
            new Grams(0.6));

        // Act
        var document = MealItemDocumentMapper.ToDocument(item);

        // Assert
        document.FoodId.Should().Be(item.FoodId.Value);
        document.Quantity.Should().Be(200);
        document.Unit.Should().Be(nameof(MeasurementUnit.Gram));
        document.Calories.Should().Be(260);
        document.Protein.Should().Be(5.4);
        document.Carbs.Should().Be(56);
        document.Fat.Should().Be(0.6);
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
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
        roundTrippedItem.Id.Should().Be(id);
        roundTrippedItem.FoodId.Should().Be(originalItem.FoodId);
        roundTrippedItem.Quantity.Should().Be(originalItem.Quantity);
        roundTrippedItem.Unit.Should().Be(originalItem.Unit);
        roundTrippedItem.Calories.Should().Be(originalItem.Calories);
        roundTrippedItem.Protein.Should().Be(originalItem.Protein);
        roundTrippedItem.Carbs.Should().Be(originalItem.Carbs);
        roundTrippedItem.Fat.Should().Be(originalItem.Fat);
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
}
