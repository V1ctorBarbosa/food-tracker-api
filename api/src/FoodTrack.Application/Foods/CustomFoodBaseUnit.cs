using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Application.Foods;

/// <summary>Reads the base unit a client asked for on a custom food: per 100 g for a food, per 100 ml for a drink.</summary>
public static class CustomFoodBaseUnit
{
    public const double Quantity = 100;

    /// <param name="requestedUnit">"Gram" or "Milliliter", in any case.</param>
    /// <param name="unitWhenMissing">
    /// Used when <paramref name="requestedUnit"/> is null, so clients that predate the field keep working.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="requestedUnit"/> is any other value.</exception>
    public static BaseUnit Resolve(string? requestedUnit, MeasurementUnit unitWhenMissing)
    {
        if (requestedUnit is null)
        {
            return new BaseUnit(Quantity, unitWhenMissing);
        }

        if (IsUnitName(requestedUnit, MeasurementUnit.Gram))
        {
            return new BaseUnit(Quantity, MeasurementUnit.Gram);
        }

        if (IsUnitName(requestedUnit, MeasurementUnit.Milliliter))
        {
            return new BaseUnit(Quantity, MeasurementUnit.Milliliter);
        }

        throw new ArgumentException(
            $"Base unit must be {nameof(MeasurementUnit.Gram)} or {nameof(MeasurementUnit.Milliliter)}, got '{requestedUnit}'.",
            nameof(requestedUnit));
    }

    // Compared by name rather than Enum.TryParse, which would also accept numbers such as "0".
    private static bool IsUnitName(string requestedUnit, MeasurementUnit unit) =>
        string.Equals(requestedUnit.Trim(), unit.ToString(), StringComparison.OrdinalIgnoreCase);
}
