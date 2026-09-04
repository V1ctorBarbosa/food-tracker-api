using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Tests.Infrastructure.Foods;

public sealed class FoodServingDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromFoodServing()
    {
        // Arrange
        var serving = FoodServing.Create(FoodServingId.NewId(), "can", 350, MeasurementUnit.Milliliter);

        // Act
        var document = FoodServingDocumentMapper.ToDocument(serving);

        // Assert
        document.Name.Should().Be("can");
        document.Quantity.Should().Be(350);
        document.Unit.Should().Be(nameof(MeasurementUnit.Milliliter));
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = FoodServingId.NewId();
        var originalServing = FoodServing.Create(id, "can", 350, MeasurementUnit.Milliliter);

        // Act
        var document = FoodServingDocumentMapper.ToDocument(originalServing);
        var roundTrippedServing = FoodServingDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedServing.Id.Should().Be(id);
        roundTrippedServing.Name.Should().Be(originalServing.Name);
        roundTrippedServing.Quantity.Should().Be(originalServing.Quantity);
        roundTrippedServing.Unit.Should().Be(originalServing.Unit);
    }
}
