using FoodTrack.Application.Common;
using FoodTrack.Application.Foods;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Meals;

public sealed class MealService
{
    private readonly IMealRepository _mealRepository;
    private readonly IFoodRepository _foodRepository;
    private readonly MealPhotoLifecycle _mealPhotoLifecycle;

    public MealService(IMealRepository mealRepository, IFoodRepository foodRepository, MealPhotoLifecycle mealPhotoLifecycle)
    {
        _mealRepository = mealRepository;
        _foodRepository = foodRepository;
        _mealPhotoLifecycle = mealPhotoLifecycle;
    }

    public async Task<IReadOnlyList<MealSummaryDto>> ListAsync(
        UserId userId,
        DayRange dayRange,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        var summaries = await _mealRepository.ListSummariesByUserAsync(userId, dayRange, utcOffset, cancellationToken);
        return summaries.Select(ToDto).ToList();
    }

    public async Task<MealDto?> GetByIdAsync(UserId userId, MealId id, CancellationToken cancellationToken = default)
    {
        var meal = await _mealRepository.GetByIdAsync(id, userId, cancellationToken);
        if (meal is null)
        {
            return null;
        }

        return await ToDtoAsync(meal, cancellationToken);
    }

    public async Task<MealDto> CreateAsync(UserId userId, CreateMealRequest request, CancellationToken cancellationToken = default)
    {
        var items = await ResolveItemsAsync(userId, request.Items, cancellationToken);
        var mealType = Enum.Parse<MealType>(request.MealType, ignoreCase: true);
        var loggedAt = LoggedAtPolicy.Resolve(request.LoggedAt, DateTimeOffset.UtcNow);
        var captures = MealRequestMapper.ToCaptures(request.Captures ?? []);
        var meal = Meal.Create(MealId.NewId(), userId, loggedAt, mealType, items, captures);

        await _mealPhotoLifecycle.EnsureNewPhotosAreUploadedAsync(userId, [], meal.Captures, cancellationToken);
        await _mealRepository.AddAsync(meal, cancellationToken);
        await _mealPhotoLifecycle.SyncAfterSaveAsync(userId, [], meal.Captures, cancellationToken);

        return await ToDtoAsync(meal, cancellationToken);
    }

    /// <returns>The replaced meal, or null when it does not exist or does not belong to <paramref name="userId"/>.</returns>
    public async Task<MealDto?> UpdateAsync(
        UserId userId,
        MealId id,
        UpdateMealRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _mealRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var items = await ResolveItemsAsync(userId, request.Items, cancellationToken);
        var mealType = Enum.Parse<MealType>(request.MealType, ignoreCase: true);
        var captures = ResolveUpdatedCaptures(existing, request.Captures);
        var meal = Meal.Create(id, userId, existing.LoggedAt, mealType, items, captures);

        await _mealPhotoLifecycle.EnsureNewPhotosAreUploadedAsync(userId, existing.Captures, meal.Captures, cancellationToken);
        await _mealRepository.UpdateAsync(meal, cancellationToken);
        await _mealPhotoLifecycle.SyncAfterSaveAsync(userId, existing.Captures, meal.Captures, cancellationToken);

        return await ToDtoAsync(meal, cancellationToken);
    }

    /// <returns>True when the meal existed and belonged to <paramref name="userId"/> and was deleted.</returns>
    public async Task<bool> DeleteAsync(UserId userId, MealId id, CancellationToken cancellationToken = default)
    {
        var existing = await _mealRepository.GetByIdAsync(id, userId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await _mealRepository.DeleteAsync(id, cancellationToken);
        await _mealPhotoLifecycle.DeletePhotosOfAsync(existing, cancellationToken);
        return true;
    }

    private async Task<MealDto> ToDtoAsync(Meal meal, CancellationToken cancellationToken)
    {
        var photoReadUrls = await _mealPhotoLifecycle.GetReadUrlsAsync(meal, cancellationToken);
        return MealDtoMapper.ToDto(meal, photoReadUrls);
    }

    private async Task<IReadOnlyList<MealItem>> ResolveItemsAsync(
        UserId userId,
        IReadOnlyList<MealItemRequest> itemRequests,
        CancellationToken cancellationToken)
    {
        Meal.EnsureItemCountIsAllowed(itemRequests.Count);

        var items = new List<MealItem>(itemRequests.Count);
        foreach (var itemRequest in itemRequests)
        {
            items.Add(await ResolveItemAsync(userId, itemRequest, cancellationToken));
        }

        return items;
    }

    private async Task<MealItem> ResolveItemAsync(UserId userId, MealItemRequest itemRequest, CancellationToken cancellationToken)
    {
        var hasFoodId = !string.IsNullOrWhiteSpace(itemRequest.FoodId);

        if (itemRequest.EstimatedFood is { } estimatedFoodRequest)
        {
            EnsureFoodIdIsAbsent(hasFoodId);
            return MealRequestMapper.ToEstimatedItem(estimatedFoodRequest, itemRequest.Quantity, itemRequest.Unit);
        }

        if (!hasFoodId || itemRequest.FoodId is not { } foodId)
        {
            throw new ArgumentException("Each meal item needs a food id or an estimated food.", nameof(itemRequest));
        }

        var food = await FindFoodVisibleToAsync(userId, foodId, cancellationToken);
        var unit = MealRequestMapper.ParseUnit(itemRequest.Unit);

        return MealItem.FromCatalogFood(MealItemId.NewId(), food, itemRequest.Quantity, unit);
    }

    private static void EnsureFoodIdIsAbsent(bool hasFoodId)
    {
        if (hasFoodId)
        {
            throw new ArgumentException("A meal item cannot have both a food id and an estimated food.", nameof(hasFoodId));
        }
    }

    /// <summary>
    /// Resolves a food id sent by the client. Every food id is a GUID minted by the API, so anything
    /// else is rejected before it can reach the database as a document path. Another user's custom
    /// food is reported as missing too, so its id cannot be used to read its nutrition values.
    /// </summary>
    private async Task<Food> FindFoodVisibleToAsync(UserId userId, string requestedFoodId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestedFoodId, out _))
        {
            throw new ArgumentException($"Food '{requestedFoodId}' does not exist.", nameof(requestedFoodId));
        }

        var food = await _foodRepository.GetByIdAsync(new FoodId(requestedFoodId), cancellationToken);
        if (food is null || !food.IsVisibleTo(userId))
        {
            throw new ArgumentException($"Food '{requestedFoodId}' does not exist.", nameof(requestedFoodId));
        }

        return food;
    }

    private static IReadOnlyList<MealCapture> ResolveUpdatedCaptures(Meal existing, IReadOnlyList<MealCaptureRequest>? captureRequests)
    {
        if (captureRequests is null)
        {
            return existing.Captures;
        }

        return MealRequestMapper.ToCaptures(captureRequests);
    }

    private static MealSummaryDto ToDto(MealSummary summary) => MealDtoMapper.ToDto(summary);
}
