using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;

namespace FoodTrack.Infrastructure.Users;

/// <summary>
/// Pure mapping between the domain's <see cref="User"/> aggregate and its Firestore
/// persistence representation. Kept free of any Firestore I/O so it can be unit tested
/// without a live database.
/// </summary>
internal static class UserDocumentMapper
{
    public static User ToDomain(UserId id, UserDocument document)
    {
        Kilograms? targetWeight = null;
        if (document.TargetWeightKilograms is { } weight)
        {
            targetWeight = new Kilograms(weight);
        }

        Milliliters? dailyWaterTarget = null;
        if (document.DailyWaterTargetMilliliters is { } water)
        {
            dailyWaterTarget = new Milliliters(water);
        }

        Grams? dailyProteinTarget = null;
        if (document.DailyProteinTargetGrams is { } protein)
        {
            dailyProteinTarget = new Grams(protein);
        }

        Grams? dailyCarbsTarget = null;
        if (document.DailyCarbsTargetGrams is { } carbs)
        {
            dailyCarbsTarget = new Grams(carbs);
        }

        Grams? dailyFatTarget = null;
        if (document.DailyFatTargetGrams is { } fat)
        {
            dailyFatTarget = new Grams(fat);
        }

        return User.Create(
            id,
            document.Name,
            new Age(document.Age),
            new Centimeters(document.HeightCentimeters),
            Enum.Parse<Gender>(document.Gender),
            Enum.Parse<ActivityLevel>(document.ActivityLevel),
            new Kilocalories(document.DailyCalorieTarget),
            new Kilocalories(document.DesiredCalorieDeficit),
            targetWeight,
            dailyWaterTarget,
            dailyProteinTarget,
            dailyCarbsTarget,
            dailyFatTarget);
    }

    public static UserDocument ToDocument(User user) => new()
    {
        Name = user.Name,
        Age = user.Age.Value,
        HeightCentimeters = user.Height.Value,
        Gender = user.Gender.ToString(),
        ActivityLevel = user.ActivityLevel.ToString(),
        TargetWeightKilograms = user.TargetWeight?.Value,
        DesiredCalorieDeficit = user.DesiredCalorieDeficit.Value,
        DailyCalorieTarget = user.DailyCalorieTarget.Value,
        DailyWaterTargetMilliliters = user.DailyWaterTarget?.Value,
        DailyProteinTargetGrams = user.DailyProteinTarget?.Value,
        DailyCarbsTargetGrams = user.DailyCarbsTarget?.Value,
        DailyFatTargetGrams = user.DailyFatTarget?.Value,
    };
}
