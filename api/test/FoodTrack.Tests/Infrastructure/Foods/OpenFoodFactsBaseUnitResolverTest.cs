using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Tests.Infrastructure.Foods;

public sealed class OpenFoodFactsBaseUnitResolverTest
{
    private static readonly string[] NoCategories = [];

    // Package quantities as Open Food Facts returned them for Brazilian products.
    [Theory]
    [InlineData("350ml")]
    [InlineData("310ml")]
    [InlineData("3 l")]
    [InlineData("300 mL")]
    [InlineData("1L")]
    [InlineData("1 L")]
    [InlineData("6 x 350 ml")]
    [InlineData("1,5 L")]
    [InlineData("33 cl")]
    [InlineData("Lata 350ml")]
    public void Resolve_ReturnsMilliliter_WhenThePackageIsSoldByVolume(string quantity)
    {
        // Act
        var unit = OpenFoodFactsBaseUnitResolver.Resolve(quantity, NoCategories);

        // Assert
        unit.Should().Be(MeasurementUnit.Milliliter);
    }

    [Theory]
    [InlineData("500 g")]
    [InlineData("1kg")]
    [InlineData("200g")]
    [InlineData("25 G")]
    public void Resolve_ReturnsGram_WhenThePackageIsSoldByMass(string quantity)
    {
        // Act
        var unit = OpenFoodFactsBaseUnitResolver.Resolve(quantity, NoCategories);

        // Assert
        unit.Should().Be(MeasurementUnit.Gram);
    }

    [Fact]
    public void Resolve_TrustsTheQuantity_OverTheBeveragesCategory()
    {
        // Arrange: a powdered drink mix is filed under beverages but sold, and measured, by mass.
        string[] beverageCategories = ["en:beverages", "en:beverages-and-beverages-preparations"];

        // Act
        var unit = OpenFoodFactsBaseUnitResolver.Resolve("25 g", beverageCategories);

        // Assert
        unit.Should().Be(MeasurementUnit.Gram);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1 lata")]
    [InlineData("6 unidades")]
    public void Resolve_FallsBackToTheBeveragesCategory_WhenTheQuantityHasNoUnit(string? quantity)
    {
        // Arrange
        string[] categories = ["en:plant-based-foods-and-beverages", "en:beverages", "en:juices-and-nectars"];

        // Act
        var unit = OpenFoodFactsBaseUnitResolver.Resolve(quantity, categories);

        // Assert
        unit.Should().Be(MeasurementUnit.Milliliter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1 lata")]
    public void Resolve_ReturnsGram_WhenNeitherTheQuantityNorTheCategoriesSayItIsADrink(string? quantity)
    {
        // Arrange
        string[] categories = ["en:snacks", "en:biscuits"];

        // Act
        var unit = OpenFoodFactsBaseUnitResolver.Resolve(quantity, categories);

        // Assert
        unit.Should().Be(MeasurementUnit.Gram);
    }
}
