using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Users;

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

    /// <summary>Lists every cached food that came from <paramref name="source"/>, servings left out.</summary>
    Task<IReadOnlyList<Food>> ListBySourceAsync(FoodSource source, CancellationToken cancellationToken = default);

    /// <summary>Lists the custom foods the user created, in no particular order.</summary>
    Task<IReadOnlyList<Food>> ListCustomFoodsOwnedByAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the custom foods the user created, servings included. The shared cache is untouched.</summary>
    Task DeleteCustomFoodsOwnedByAsync(UserId userId, CancellationToken cancellationToken = default);
}
