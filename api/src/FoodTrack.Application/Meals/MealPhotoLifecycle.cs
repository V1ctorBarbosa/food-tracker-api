using FoodTrack.Application.MealEstimates;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Meals;

/// <summary>
/// Keeps stored photos in step with the captures a meal is saved with: a capture claiming a photo
/// must have one, saving a meal keeps its photos for good, and a capture that leaves the meal, or
/// a meal that is deleted, takes its photo with it.
/// </summary>
public sealed class MealPhotoLifecycle
{
    private readonly IMealPhotoStorage _mealPhotoStorage;

    public MealPhotoLifecycle(IMealPhotoStorage mealPhotoStorage)
    {
        _mealPhotoStorage = mealPhotoStorage;
    }

    /// <exception cref="ArgumentException">A capture claims a photo that was never uploaded, has expired or is too large.</exception>
    public async Task EnsureNewPhotosAreUploadedAsync(
        UserId userId,
        IReadOnlyList<MealCapture> previousCaptures,
        IReadOnlyList<MealCapture> captures,
        CancellationToken cancellationToken = default)
    {
        foreach (var capture in FindNewCapturesWithPhoto(previousCaptures, captures))
        {
            var photoState = await _mealPhotoStorage.GetStateAsync(userId, capture.Id, cancellationToken);
            if (photoState is StoredPhotoState.Missing or StoredPhotoState.TooLarge)
            {
                throw new ArgumentException($"Capture '{capture.Id.Value}' has no usable photo.", nameof(captures));
            }
        }
    }

    /// <summary>Attaches the photos of new captures and deletes those of captures no longer on the meal.</summary>
    public async Task SyncAfterSaveAsync(
        UserId userId,
        IReadOnlyList<MealCapture> previousCaptures,
        IReadOnlyList<MealCapture> captures,
        CancellationToken cancellationToken = default)
    {
        foreach (var capture in FindNewCapturesWithPhoto(previousCaptures, captures))
        {
            await _mealPhotoStorage.MarkAttachedAsync(userId, capture.Id, cancellationToken);
        }

        foreach (var capture in FindRemovedCapturesWithPhoto(previousCaptures, captures))
        {
            await _mealPhotoStorage.DeleteAsync(userId, capture.Id, cancellationToken);
        }
    }

    public async Task DeletePhotosOfAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        foreach (var capture in meal.Captures.Where(capture => capture.HasPhoto))
        {
            await _mealPhotoStorage.DeleteAsync(meal.UserId, capture.Id, cancellationToken);
        }
    }

    public async Task<IReadOnlyDictionary<CaptureId, Uri>> GetReadUrlsAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        var readUrls = new Dictionary<CaptureId, Uri>();
        foreach (var capture in meal.Captures.Where(capture => capture.HasPhoto))
        {
            readUrls[capture.Id] = await _mealPhotoStorage.GetAttachedPhotoReadUrlAsync(meal.UserId, capture.Id, cancellationToken);
        }

        return readUrls;
    }

    /// <returns>The captures with a photo in <paramref name="captures"/> that <paramref name="previousCaptures"/> does not have.</returns>
    public static IReadOnlyList<MealCapture> FindNewCapturesWithPhoto(
        IReadOnlyList<MealCapture> previousCaptures,
        IReadOnlyList<MealCapture> captures)
    {
        var previousCaptureIds = previousCaptures.Select(capture => capture.Id).ToHashSet();

        return captures
            .Where(capture => capture.HasPhoto && !previousCaptureIds.Contains(capture.Id))
            .ToList();
    }

    /// <returns>The captures with a photo in <paramref name="previousCaptures"/> that <paramref name="captures"/> no longer has.</returns>
    public static IReadOnlyList<MealCapture> FindRemovedCapturesWithPhoto(
        IReadOnlyList<MealCapture> previousCaptures,
        IReadOnlyList<MealCapture> captures) =>
        FindNewCapturesWithPhoto(captures, previousCaptures);
}
