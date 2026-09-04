using FluentAssertions;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using NSubstitute;

namespace FoodTrack.Tests.Application.Foods;

public sealed class FoodServiceTest
{
    private readonly IFoodRepository _foodRepository = Substitute.For<IFoodRepository>();
    private readonly FoodService _sut;

    public FoodServiceTest()
    {
        _sut = new FoodService(_foodRepository);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenFoodDoesNotExist()
    {
        // Arrange
        var id = FoodId.NewId();
        _foodRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Food?)null);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDto_WithServings_WhenFoodExists()
    {
        // Arrange
        var servingId = FoodServingId.NewId();
        var serving = FoodServing.Create(servingId, "can", 350, MeasurementUnit.Milliliter);
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

        _foodRepository.GetByIdAsync(food.Id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.GetByIdAsync(food.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Coca-Cola");
        result.Source.Should().Be(nameof(FoodSource.Usda));
        result.BaseUnitQuantity.Should().Be(100);
        result.BaseUnitType.Should().Be(nameof(MeasurementUnit.Milliliter));
        result.CaloriesPerBaseUnit.Should().Be(42);
        result.Servings.Should().ContainSingle();
        result.Servings[0].Id.Should().Be(servingId.Value);
        result.Servings[0].Name.Should().Be("can");
        result.Servings[0].Quantity.Should().Be(350);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMappedDtos_FromRepository()
    {
        // Arrange
        var food = Food.Create(
            FoodId.NewId(),
            "Chicken breast",
            FoodSource.Usda,
            "171077",
            new BaseUnit(100, MeasurementUnit.Gram),
            new Kilocalories(165),
            new Grams(31),
            new Grams(0),
            new Grams(3.6));

        _foodRepository.SearchByNameAsync("chicken", Arg.Any<CancellationToken>()).Returns([food]);

        // Act
        var result = await _sut.SearchAsync("chicken");

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Chicken breast");
    }
}
