namespace FoodTrack.Domain.Meals;

/// <summary>
/// What one AI estimate was made from: a photo, a description, or both. The same value is the
/// input of an estimate and, once its items are saved, the record on the meal of where they came
/// from.
/// </summary>
public sealed record MealCapture
{
    /// <summary>
    /// Without a photo the description is all the model has, and a word like "almoço" cannot be
    /// estimated. With a photo any description only refines it.
    /// </summary>
    public const int MinimumDescriptionLengthWithoutPhoto = 10;

    public CaptureId Id { get; }

    public bool HasPhoto { get; }

    public MealDescription? Description { get; }

    private MealCapture(CaptureId id, bool hasPhoto, MealDescription? description)
    {
        Id = id;
        HasPhoto = hasPhoto;
        Description = description;
    }

    /// <exception cref="ArgumentException">
    /// There is no photo and the description is missing or shorter than <see cref="MinimumDescriptionLengthWithoutPhoto"/>.
    /// </exception>
    public static MealCapture Create(CaptureId id, bool hasPhoto, MealDescription? description)
    {
        if (!hasPhoto)
        {
            EnsureDescriptionCanStandAlone(description);
        }

        return new MealCapture(id, hasPhoto, description);
    }

    private static void EnsureDescriptionCanStandAlone(MealDescription? description)
    {
        if (description is not { } presentDescription)
        {
            throw new ArgumentException("A capture needs a photo or a description.", nameof(description));
        }

        if (presentDescription.Value.Length < MinimumDescriptionLengthWithoutPhoto)
        {
            throw new ArgumentException(
                $"Without a photo the description must have at least {MinimumDescriptionLengthWithoutPhoto} characters.",
                nameof(description));
        }
    }
}
