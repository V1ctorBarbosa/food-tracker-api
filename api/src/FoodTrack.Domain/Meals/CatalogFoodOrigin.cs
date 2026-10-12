using FoodTrack.Domain.Foods;

namespace FoodTrack.Domain.Meals;

/// <summary>The item was logged from a <see cref="Food"/> in the catalog (TACO, Open Food Facts or a custom food).</summary>
public sealed record CatalogFoodOrigin(FoodId FoodId) : MealItemOrigin;
