using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Infrastructure.Foods;

internal static class FoodServingDocumentMapper
{
    public static FoodServing ToDomain(FoodServingId id, FoodServingDocument document) => FoodServing.Create(
        id,
        document.Name,
        document.Quantity,
        Enum.Parse<MeasurementUnit>(document.Unit));

    public static FoodServingDocument ToDocument(FoodServing serving) => new()
    {
        Name = serving.Name,
        Quantity = serving.Quantity,
        Unit = serving.Unit.ToString(),
    };
}
