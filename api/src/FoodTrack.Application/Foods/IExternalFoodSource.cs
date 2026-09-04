using FoodTrack.Domain.Foods;

namespace FoodTrack.Application.Foods;

/// <summary>
/// A reliable external nutritional data source (docs section 14) consulted when the local
/// Firestore cache has no match for a search. Implementations are responsible for their own
/// wire format entirely -- this port speaks only in domain terms.
/// </summary>
public interface IExternalFoodSource
{
    Task<IReadOnlyList<Food>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
