namespace FoodTrack.Domain.Meals;

/// <summary>
/// Where a <see cref="MealItem"/>'s nutrition came from. Closed to the two cases in this assembly,
/// <see cref="CatalogFoodOrigin"/> and <see cref="EstimatedFoodOrigin"/>, so every consumer can
/// rely on handling exactly those.
/// </summary>
public abstract record MealItemOrigin
{
    private protected MealItemOrigin()
    {
    }
}
