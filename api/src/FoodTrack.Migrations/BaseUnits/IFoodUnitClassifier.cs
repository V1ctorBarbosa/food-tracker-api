using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Migrations.BaseUnits;

/// <summary>Tells whether a cached food from one external source is measured in grams or milliliters.</summary>
internal interface IFoodUnitClassifier
{
    FoodSource Source { get; }

    /// <returns>The unit the food should be measured in, or null when the source has no answer for it.</returns>
    Task<MeasurementUnit?> ClassifyAsync(Food food, CancellationToken cancellationToken = default);
}
