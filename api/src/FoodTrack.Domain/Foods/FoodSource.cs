namespace FoodTrack.Domain.Foods;

public enum FoodSource
{
    /// <summary>No longer actively queried -- kept so a food cached from an earlier USDA search still parses.</summary>
    Usda,
    OpenFoodFacts,
    Custom,
    Taco,
}
