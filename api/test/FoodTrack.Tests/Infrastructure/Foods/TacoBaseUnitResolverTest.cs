using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Tests.Infrastructure.Foods;

public sealed class TacoBaseUnitResolverTest
{
    private const string BeveragesCategory = "Bebidas (alcoólicas e não alcoólicas)";
    private const string FruitsCategory = "Frutas e derivados";
    private const string DairyCategory = "Leite e derivados";

    // Ids and categories as TACO lists them.
    [Theory]
    [InlineData(480, BeveragesCategory)] // Refrigerante, tipo cola
    [InlineData(471, BeveragesCategory)] // Café, infusão 10%
    [InlineData(215, FruitsCategory)] // Laranja, pêra, suco
    [InlineData(458, DairyCategory)] // Leite, de vaca, integral
    [InlineData(457, DairyCategory)] // Leite, de vaca, desnatado, UHT
    public void Resolve_ReturnsMilliliter_ForDrinks(int tacoId, string category)
    {
        // Act
        var unit = TacoBaseUnitResolver.Resolve(tacoId, category);

        // Assert
        unit.Should().Be(MeasurementUnit.Milliliter);
    }

    [Theory]
    [InlineData(1, "Cereais e derivados")] // Arroz, integral, cozido
    [InlineData(448, DairyCategory)] // Iogurte, natural
    [InlineData(459, DairyCategory)] // Leite, de vaca, integral, pó
    [InlineData(453, DairyCategory)] // Leite, condensado
    [InlineData(214, FruitsCategory)] // Laranja, pêra, crua
    [InlineData(325, "Carnes e derivados")] // Caldo de galinha, tablete
    public void Resolve_ReturnsGram_ForFoods(int tacoId, string category)
    {
        // Act
        var unit = TacoBaseUnitResolver.Resolve(tacoId, category);

        // Assert
        unit.Should().Be(MeasurementUnit.Gram);
    }

    [Fact]
    public void Resolve_ReturnsGram_WhenTheCategoryIsMissing()
    {
        // Act
        var unit = TacoBaseUnitResolver.Resolve(1, null);

        // Assert
        unit.Should().Be(MeasurementUnit.Gram);
    }
}
