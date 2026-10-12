using FoodTrack.Application.Meals;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.MealEstimates;

/// <summary>
/// Estimates a meal from a photo, a description, or both. Nothing is saved here: the items come
/// back to the client, which reviews them and saves the ones it keeps through the meal endpoints.
/// </summary>
public sealed class MealEstimateService
{
    private readonly IMealEstimator _mealEstimator;
    private readonly IMealPhotoStorage _mealPhotoStorage;
    private readonly IMealEstimateAccessPolicy _accessPolicy;

    public MealEstimateService(IMealEstimator mealEstimator, IMealPhotoStorage mealPhotoStorage, IMealEstimateAccessPolicy accessPolicy)
    {
        _mealEstimator = mealEstimator;
        _mealPhotoStorage = mealPhotoStorage;
        _accessPolicy = accessPolicy;
    }

    /// <exception cref="MealEstimateAccessDeniedException">The user may not use AI estimates.</exception>
    public async Task<PhotoUploadSlotDto> CreateUploadSlotAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        EnsureAccess(userId);

        var captureId = CaptureId.NewId();
        var slot = await _mealPhotoStorage.CreateUploadSlotAsync(userId, captureId, cancellationToken);

        return new PhotoUploadSlotDto(captureId.Value, slot.UploadUrl.ToString(), slot.Headers, slot.ExpiresAt);
    }

    /// <exception cref="MealEstimateAccessDeniedException">The user may not use AI estimates.</exception>
    /// <exception cref="ArgumentException">The request has no usable photo and no long enough description.</exception>
    /// <exception cref="NoFoodRecognizedException">The estimator found no food.</exception>
    /// <exception cref="MealEstimatorUnavailableException">The estimator failed or gave no usable food.</exception>
    public async Task<MealEstimateDto> EstimateAsync(UserId userId, EstimateMealRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAccess(userId);

        var capture = ToCapture(request);
        Uri? photoUrl = null;
        if (capture.HasPhoto)
        {
            photoUrl = await GetUploadedPhotoUrlAsync(userId, capture.Id, cancellationToken);
        }

        var recognizedMeal = await _mealEstimator.EstimateAsync(new MealEstimateInput(photoUrl, capture.Description), cancellationToken);
        var items = ToItems(recognizedMeal, capture.Id);

        return new MealEstimateDto(
            capture.Id.Value,
            capture.HasPhoto,
            capture.Description?.Value,
            items.Select(ToDto).ToList());
    }

    private void EnsureAccess(UserId userId)
    {
        if (!_accessPolicy.CanEstimate(userId))
        {
            throw new MealEstimateAccessDeniedException();
        }
    }

    /// <summary>
    /// A photo estimate reuses the capture of its upload slot; a description-only estimate has
    /// none yet, so it gets a new one here.
    /// </summary>
    private static MealCapture ToCapture(EstimateMealRequest request)
    {
        MealDescription? description = null;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            description = new MealDescription(request.Description);
        }

        if (string.IsNullOrWhiteSpace(request.CaptureId))
        {
            return MealCapture.Create(CaptureId.NewId(), hasPhoto: false, description);
        }

        return MealCapture.Create(new CaptureId(request.CaptureId), hasPhoto: true, description);
    }

    private async Task<Uri> GetUploadedPhotoUrlAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken)
    {
        var photoState = await _mealPhotoStorage.GetStateAsync(userId, captureId, cancellationToken);
        if (photoState == StoredPhotoState.Missing)
        {
            throw new ArgumentException("The photo for this capture was not uploaded, or it has expired.", nameof(captureId));
        }

        if (photoState == StoredPhotoState.TooLarge)
        {
            throw new ArgumentException("The photo for this capture is too large.", nameof(captureId));
        }

        if (photoState == StoredPhotoState.Attached)
        {
            throw new ArgumentException("The photo for this capture already belongs to a saved meal.", nameof(captureId));
        }

        return await _mealPhotoStorage.GetPendingPhotoReadUrlAsync(userId, captureId, cancellationToken);
    }

    private static IReadOnlyList<MealItem> ToItems(RecognizedMeal recognizedMeal, CaptureId captureId)
    {
        if (recognizedMeal.Foods.Count == 0)
        {
            throw new NoFoodRecognizedException();
        }

        var items = RecognizedFoodMapper.ToValidItems(recognizedMeal.Foods, captureId);
        if (items.Count == 0)
        {
            throw new MealEstimatorUnavailableException("The estimator answered, but none of its foods had valid values.");
        }

        return items;
    }

    private static EstimatedMealItemDto ToDto(MealItem item)
    {
        var mealItemDto = MealDtoMapper.ToDto(item);
        if (mealItemDto.EstimatedFood is not { } estimatedFood)
        {
            throw new InvalidOperationException("An estimate can only produce estimated items.");
        }

        return new EstimatedMealItemDto(
            mealItemDto.Quantity,
            mealItemDto.Unit,
            mealItemDto.Calories,
            mealItemDto.Protein,
            mealItemDto.Carbs,
            mealItemDto.Fat,
            estimatedFood);
    }
}
