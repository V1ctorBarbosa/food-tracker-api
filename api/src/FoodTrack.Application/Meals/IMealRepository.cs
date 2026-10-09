using FoodTrack.Application.Common;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Meals;

public interface IMealRepository
{
    /// <summary>
    /// Looks up a meal scoped to its owner. Returns null both when the id does not exist
    /// and when it belongs to a different user, so callers never learn which case it was.
    /// </summary>
    Task<Meal?> GetByIdAsync(MealId id, UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Lists the user's meals in a local-day range as summaries, without loading their items.</summary>
    Task<IReadOnlyList<MealSummary>> ListSummariesByUserAsync(
        UserId userId,
        DayRange dayRange,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default);

    /// <summary>Upserts the meal and its items as a single aggregate.</summary>
    Task AddAsync(Meal meal, CancellationToken cancellationToken = default);

    Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default);

    Task DeleteAsync(MealId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves every logged item of the food to <paramref name="category"/> with
    /// <see cref="MeasurementUnitReclassification"/>, so they match a food that changed between g and ml.
    /// Items already in that category are left alone, so running it again changes nothing.
    /// The nutritional snapshot of each item is not touched.
    /// </summary>
    Task ReclassifyItemUnitsAsync(FoodId foodId, MeasurementUnitCategory category, CancellationToken cancellationToken = default);

    /// <summary>Deletes every meal the user logged, items included.</summary>
    Task DeleteAllByUserAsync(UserId userId, CancellationToken cancellationToken = default);
}
