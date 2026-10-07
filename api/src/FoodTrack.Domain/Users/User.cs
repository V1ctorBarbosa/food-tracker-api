using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Users;

public sealed class User
{
    public const int MaximumNameLength = 100;

    public UserId Id { get; }

    public string Name { get; }

    public Age Age { get; }

    public Centimeters Height { get; }

    public Gender Gender { get; }

    public ActivityLevel ActivityLevel { get; }

    public Kilograms? TargetWeight { get; }

    public Kilocalories DesiredCalorieDeficit { get; }

    public Kilocalories DailyCalorieTarget { get; }

    public Milliliters? DailyWaterTarget { get; }

    public Grams? DailyProteinTarget { get; }

    public Grams? DailyCarbsTarget { get; }

    public Grams? DailyFatTarget { get; }

    private User(
        UserId id,
        string name,
        Age age,
        Centimeters height,
        Gender gender,
        ActivityLevel activityLevel,
        Kilocalories dailyCalorieTarget,
        Kilocalories desiredCalorieDeficit,
        Kilograms? targetWeight,
        Milliliters? dailyWaterTarget,
        Grams? dailyProteinTarget,
        Grams? dailyCarbsTarget,
        Grams? dailyFatTarget)
    {
        Id = id;
        Name = name;
        Age = age;
        Height = height;
        Gender = gender;
        ActivityLevel = activityLevel;
        DailyCalorieTarget = dailyCalorieTarget;
        DesiredCalorieDeficit = desiredCalorieDeficit;
        TargetWeight = targetWeight;
        DailyWaterTarget = dailyWaterTarget;
        DailyProteinTarget = dailyProteinTarget;
        DailyCarbsTarget = dailyCarbsTarget;
        DailyFatTarget = dailyFatTarget;
    }

    public static User Create(
        UserId id,
        string name,
        Age age,
        Centimeters height,
        Gender gender,
        ActivityLevel activityLevel,
        Kilocalories dailyCalorieTarget,
        Kilocalories desiredCalorieDeficit,
        Kilograms? targetWeight = null,
        Milliliters? dailyWaterTarget = null,
        Grams? dailyProteinTarget = null,
        Grams? dailyCarbsTarget = null,
        Grams? dailyFatTarget = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (name.Trim().Length > MaximumNameLength)
        {
            throw new ArgumentException($"Name cannot be longer than {MaximumNameLength} characters.", nameof(name));
        }

        return new User(
            id,
            name.Trim(),
            age,
            height,
            gender,
            activityLevel,
            dailyCalorieTarget,
            desiredCalorieDeficit,
            targetWeight,
            dailyWaterTarget,
            dailyProteinTarget,
            dailyCarbsTarget,
            dailyFatTarget);
    }
}
