using FoodTrack.Domain.Foods;

namespace FoodTrack.Application.Foods;

public interface IFoodRepository
{
    Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken = default);

    /// <summary>Prefix match on the food name, case-insensitive, over the local cache only.</summary>
    Task<IReadOnlyList<Food>> SearchByNameAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Finds a previously-cached food by its external source and id, so a repeated
    /// search for the same external food reuses its existing FoodId instead of caching a
    /// duplicate under a freshly minted one.</summary>
    Task<Food?> GetBySourceAndExternalIdAsync(FoodSource source, string externalId, CancellationToken cancellationToken = default);

    /// <summary>Upserts the food and its servings as a single aggregate.</summary>
    Task SaveAsync(Food food, CancellationToken cancellationToken = default);
}
