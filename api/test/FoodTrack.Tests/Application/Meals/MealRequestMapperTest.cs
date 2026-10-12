using FluentAssertions;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Meals;

namespace FoodTrack.Tests.Application.Meals;

public sealed class MealRequestMapperTest
{
    [Fact]
    public void ToCapture_MapsAllFields()
    {
        // Arrange
        var captureId = CaptureId.NewId();
        var request = new MealCaptureRequest(captureId.Value, true, "o frango é empanado");

        // Act
        var capture = MealRequestMapper.ToCapture(request);

        // Assert
        capture.Id.Should().Be(captureId);
        capture.HasPhoto.Should().BeTrue();
        capture.Description.Should().Be(new MealDescription("o frango é empanado"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToCapture_HasNoDescription_WhenTheRequestLeavesItBlank(string? description)
    {
        // Arrange
        var request = new MealCaptureRequest(CaptureId.NewId().Value, true, description);

        // Act
        var capture = MealRequestMapper.ToCapture(request);

        // Assert
        capture.Description.Should().BeNull();
    }

    [Fact]
    public void ToCapture_Throws_WhenTheIdIsNotAGuid()
    {
        // Arrange
        var request = new MealCaptureRequest("../other-user/photo", true, null);

        // Act
        var act = () => MealRequestMapper.ToCapture(request);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToEstimatedItem_RecalculatesTheSnapshot_FromTheValuesPerBaseUnit()
    {
        // Arrange
        var captureId = CaptureId.NewId();
        var request = CreateChickenRequest(captureId.Value);

        // Act
        var item = MealRequestMapper.ToEstimatedItem(request, 300, "gram");

        // Assert
        var origin = item.Origin.Should().BeOfType<EstimatedFoodOrigin>().Subject;
        origin.CaptureId.Should().Be(captureId);
        origin.Food.Name.Should().Be("Peito de frango grelhado");
        item.Quantity.Should().Be(300);
        item.Unit.Should().Be(MeasurementUnit.Gram);
        item.Calories.Value.Should().Be(480);
        item.Protein.Value.Should().BeApproximately(96, 0.0001);
    }

    [Theory]
    [InlineData("Kilogram")]
    [InlineData("cup")]
    [InlineData("")]
    public void ToEstimatedItem_Throws_WhenTheBaseUnitIsNotGramOrMilliliter(string baseUnit)
    {
        // Arrange
        var request = CreateChickenRequest(CaptureId.NewId().Value) with { BaseUnit = baseUnit };

        // Act
        var act = () => MealRequestMapper.ToEstimatedItem(request, 150, nameof(MeasurementUnit.Gram));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToEstimatedItem_Throws_WhenTheNutritionIsImpossible()
    {
        // Arrange
        var request = CreateChickenRequest(CaptureId.NewId().Value) with { CaloriesPerBaseUnit = 5_000 };

        // Act
        var act = () => MealRequestMapper.ToEstimatedItem(request, 150, nameof(MeasurementUnit.Gram));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("gram", MeasurementUnit.Gram)]
    [InlineData("Milliliter", MeasurementUnit.Milliliter)]
    public void ParseUnit_ReadsTheUnitIgnoringCase(string unit, MeasurementUnit expectedUnit)
    {
        // Act
        var parsedUnit = MealRequestMapper.ParseUnit(unit);

        // Assert
        parsedUnit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cup")]
    public void ParseUnit_Throws_WhenTheUnitIsMissingOrUnknown(string? unit)
    {
        // Act
        var act = () => MealRequestMapper.ParseUnit(unit!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static EstimatedFoodRequest CreateChickenRequest(string captureId) => new(
        "Peito de frango grelhado",
        captureId,
        nameof(MeasurementUnit.Gram),
        160,
        32,
        0,
        3);
}
