using FoodTrack.Domain.Meals;

namespace FoodTrack.Infrastructure.Meals;

internal static class MealCaptureDocumentMapper
{
    public static MealCaptureDocument ToDocument(MealCapture capture) => new()
    {
        Id = capture.Id.Value,
        HasPhoto = capture.HasPhoto,
        Description = capture.Description?.Value,
    };

    public static MealCapture ToDomain(MealCaptureDocument document)
    {
        MealDescription? description = null;
        if (document.Description is { } storedDescription)
        {
            description = new MealDescription(storedDescription);
        }

        return MealCapture.Create(new CaptureId(document.Id), document.HasPhoto, description);
    }
}
