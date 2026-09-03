using FoodTrack.Domain.Common;

namespace FoodTrack.Domain.Users;

public sealed class User
{
    public UserId Id { get; }

    public string Name { get; }

    public Age Age { get; }

    public Centimeters Height { get; }

    public Sex Sex { get; }

    public ActivityLevel ActivityLevel { get; }

    public Kilograms? TargetWeight { get; }

    public Kilocalories DesiredCalorieDeficit { get; }

    public Kilocalories DailyCalorieTarget { get; }

    private User(
        UserId id,
        string name,
        Age age,
        Centimeters height,
        Sex sex,
        ActivityLevel activityLevel,
        Kilocalories dailyCalorieTarget,
        Kilocalories desiredCalorieDeficit,
        Kilograms? targetWeight)
    {
        Id = id;
        Name = name;
        Age = age;
        Height = height;
        Sex = sex;
        ActivityLevel = activityLevel;
        DailyCalorieTarget = dailyCalorieTarget;
        DesiredCalorieDeficit = desiredCalorieDeficit;
        TargetWeight = targetWeight;
    }

    public static User Create(
        UserId id,
        string name,
        Age age,
        Centimeters height,
        Sex sex,
        ActivityLevel activityLevel,
        Kilocalories dailyCalorieTarget,
        Kilocalories desiredCalorieDeficit,
        Kilograms? targetWeight = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new User(id, name.Trim(), age, height, sex, activityLevel, dailyCalorieTarget, desiredCalorieDeficit, targetWeight);
    }
}
