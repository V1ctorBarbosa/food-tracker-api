using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Tests.Infrastructure.Foods;

public sealed class FoodDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromFood()
    {
        // Arrange
        var food = Food.Create(
            FoodId.NewId(),
            "Chicken Breast",
            FoodSource.Usda,
            "171077",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(165),
            new Grams(31),
            new Grams(0),
            new Grams(3.6));

        // Act
        var document = FoodDocumentMapper.ToDocument(food);

        // Assert
        document.Name.Should().Be("Chicken Breast");
        document.NameLower.Should().Be("chicken breast");
        document.Source.Should().Be(nameof(FoodSource.Usda));
        document.ExternalId.Should().Be("171077");
        document.BaseUnitQuantity.Should().Be(100);
        document.BaseUnitType.Should().Be(nameof(MeasurementUnit.Gram));
        document.CaloriesPerBaseUnit.Should().Be(165);
        document.ProteinPerBaseUnit.Should().Be(31);
        document.CarbsPerBaseUnit.Should().Be(0);
        document.FatPerBaseUnit.Should().Be(3.6);
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = FoodId.NewId();
        var originalFood = Food.Create(
            id,
            "Yogurt",
            FoodSource.Usda,
            "54321",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(61),
            new Grams(3.5),
            new Grams(4.7),
            new Grams(3.3));

        // Act
        var document = FoodDocumentMapper.ToDocument(originalFood);
        var roundTrippedFood = FoodDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedFood.Id.Should().Be(id);
        roundTrippedFood.Name.Should().Be(originalFood.Name);
        roundTrippedFood.Source.Should().Be(originalFood.Source);
        roundTrippedFood.ExternalId.Should().Be(originalFood.ExternalId);
        roundTrippedFood.BaseUnit.Should().Be(originalFood.BaseUnit);
        roundTrippedFood.CaloriesPerBaseUnit.Should().Be(originalFood.CaloriesPerBaseUnit);
        roundTrippedFood.ProteinPerBaseUnit.Should().Be(originalFood.ProteinPerBaseUnit);
        roundTrippedFood.CarbsPerBaseUnit.Should().Be(originalFood.CarbsPerBaseUnit);
        roundTrippedFood.FatPerBaseUnit.Should().Be(originalFood.FatPerBaseUnit);
    }

    [Fact]
    public void ToDomain_MapsNullExternalId_ForCustomFoods()
    {
        // Arrange
        var id = FoodId.NewId();
        var customFood = Food.Create(
            id,
            "My whey",
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(30, MeasurementUnit.Gram),
            new Kilocalories(120),
            new Grams(24),
            new Grams(3),
            new Grams(1.5));

        // Act
        var document = FoodDocumentMapper.ToDocument(customFood);
        var roundTrippedFood = FoodDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedFood.ExternalId.Should().BeNull();
        roundTrippedFood.Source.Should().Be(FoodSource.Custom);
    }

    [Fact]
    public void OwnerId_RoundTrips_ForCustomFoods_AndStaysNull_ForCachedFoods()
    {
        // Arrange
        var ownerId = new UserId("firebase-uid-123");
        var customFoodId = FoodId.NewId();
        var customFood = Food.Create(
            customFoodId,
            "My whey",
            FoodSource.Custom,
            externalId: null,
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(120),
            new Grams(24),
            new Grams(3),
            new Grams(1.5),
            ownerId: ownerId);

        var cachedFoodId = FoodId.NewId();
        var cachedFood = Food.Create(
            cachedFoodId,
            "Chicken Breast",
            FoodSource.Usda,
            "171077",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(165),
            new Grams(31),
            new Grams(0),
            new Grams(3.6));

        // Act
        var customFoodDocument = FoodDocumentMapper.ToDocument(customFood);
        var roundTrippedCustomFood = FoodDocumentMapper.ToDomain(customFoodId, customFoodDocument);
        var cachedFoodDocument = FoodDocumentMapper.ToDocument(cachedFood);
        var roundTrippedCachedFood = FoodDocumentMapper.ToDomain(cachedFoodId, cachedFoodDocument);

        // Assert
        customFoodDocument.OwnerId.Should().Be("firebase-uid-123");
        roundTrippedCustomFood.OwnerId.Should().Be(ownerId);
        cachedFoodDocument.OwnerId.Should().BeNull();
        roundTrippedCachedFood.OwnerId.Should().BeNull();
    }
}
