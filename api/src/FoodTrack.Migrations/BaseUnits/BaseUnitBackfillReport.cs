using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Migrations.BaseUnits;

internal sealed record BaseUnitChange(Food Food, MeasurementUnit NewUnit);

internal sealed class BaseUnitBackfillReport
{
    public List<BaseUnitChange> Changed { get; } = [];

    /// <summary>Foods their source had no answer for (a product removed from Open Food Facts, a network error); left as they were.</summary>
    public List<Food> Unclassified { get; } = [];

    public int UnchangedCount { get; set; }
}
