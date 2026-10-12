namespace FoodTrack.Domain.Meals;

/// <summary>
/// Identifies one AI estimate of a meal: a photo, a description, or both. Always a GUID in its
/// canonical form, because it becomes part of the photo's storage key and free text must never
/// reach a storage path.
/// </summary>
public readonly record struct CaptureId
{
    public string Value { get; }

    public CaptureId(string value)
    {
        if (!Guid.TryParse(value, out var parsedValue))
        {
            throw new ArgumentException($"Capture id '{value}' is not a valid id.", nameof(value));
        }

        Value = parsedValue.ToString();
    }

    public static CaptureId NewId() => new(Guid.NewGuid().ToString());
}
