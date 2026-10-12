namespace FoodTrack.Infrastructure.Meals;

/// <summary>How each <see cref="Domain.Meals.MealItemOrigin"/> case is stored in <see cref="MealItemDocument.Origin"/>.</summary>
internal static class MealItemOriginNames
{
    public const string Catalog = "Catalog";
    public const string Estimated = "Estimated";
}
