using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Foods;

/// <summary>
/// A named portion of a <see cref="Food"/> (a can, a glass, a container) expressed as an
/// equivalent quantity in the food's own measurement category (docs section 17). Has no
/// meaning outside its owning food, so it is a child entity of the Food aggregate rather
/// than an independent one.
/// </summary>
public sealed class FoodServing
{
    public FoodServingId Id { get; }

    public string Name { get; }

    public double Quantity { get; }

    public MeasurementUnit Unit { get; }

    private FoodServing(FoodServingId id, string name, double quantity, MeasurementUnit unit)
    {
        Id = id;
        Name = name;
        Quantity = quantity;
        Unit = unit;
    }

    public static FoodServing Create(FoodServingId id, string name, double quantity, MeasurementUnit unit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Serving name cannot be empty.", nameof(name));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Serving quantity must be positive.");
        }

        if (unit is not (MeasurementUnit.Gram or MeasurementUnit.Milliliter))
        {
            throw new ArgumentException($"Serving unit must be Gram or Milliliter, got {unit}.", nameof(unit));
        }

        return new FoodServing(id, name.Trim(), quantity, unit);
    }
}
