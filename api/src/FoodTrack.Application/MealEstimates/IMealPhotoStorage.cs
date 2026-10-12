using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.MealEstimates;

/// <summary>
/// Where meal photos live. A photo is uploaded as pending, for an estimate that may never be
/// confirmed, and becomes attached once a meal is saved with it. Every photo is addressed by its
/// owner and capture, so one user can never reach another user's photo.
/// </summary>
public interface IMealPhotoStorage
{
    Task<PhotoUploadSlot> CreateUploadSlotAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default);

    Task<StoredPhotoState> GetStateAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default);

    /// <summary>A short-lived URL for the estimator to read a photo that is still pending.</summary>
    Task<Uri> GetPendingPhotoReadUrlAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default);

    /// <summary>A short-lived URL to show the photo of a saved meal.</summary>
    Task<Uri> GetAttachedPhotoReadUrlAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default);

    /// <summary>Keeps a pending photo for good. Does nothing for a photo that is already attached.</summary>
    Task MarkAttachedAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(UserId userId, CancellationToken cancellationToken = default);
}
