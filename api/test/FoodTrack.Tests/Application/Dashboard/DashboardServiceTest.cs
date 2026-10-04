using FluentAssertions;
using FoodTrack.Application.Common;
using FoodTrack.Application.Dashboard;
using FoodTrack.Application.Meals;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;
using FoodTrack.Domain.WeightEntries;
using NSubstitute;

namespace FoodTrack.Tests.Application.Dashboard;

public sealed class DashboardServiceTest
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMealRepository _mealRepository = Substitute.For<IMealRepository>();
    private readonly IWaterEntryRepository _waterEntryRepository = Substitute.For<IWaterEntryRepository>();
    private readonly IWeightEntryRepository _weightEntryRepository = Substitute.For<IWeightEntryRepository>();
    private readonly DashboardService _sut;
    private readonly UserId _userId = new("firebase-uid-123");
    private readonly DayRange _dayRange = new(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3));

    public DashboardServiceTest()
    {
        _sut = new DashboardService(_userRepository, _mealRepository, _waterEntryRepository, _weightEntryRepository);

        _mealRepository.ListByUserAsync(_userId, _dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        _waterEntryRepository.ListByUserAsync(_userId, _dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        _weightEntryRepository.ListByUserAsync(_userId, _dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        _weightEntryRepository.GetMostRecentAsync(_userId, Arg.Any<CancellationToken>()).Returns((WeightEntry?)null);
    }

    [Fact]
    public async Task GetDashboardAsync_PassesTheUtcOffsetToEveryRepository()
    {
        // Arrange
        var saoPaulo = TimeSpan.FromHours(-3);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());

        // Act
        await _sut.GetDashboardAsync(_userId, _dayRange, saoPaulo);

        // Assert
        await _mealRepository.Received(1).ListByUserAsync(_userId, _dayRange, saoPaulo, Arg.Any<CancellationToken>());
        await _waterEntryRepository.Received(1).ListByUserAsync(_userId, _dayRange, saoPaulo, Arg.Any<CancellationToken>());
        await _weightEntryRepository.Received(1).ListByUserAsync(_userId, _dayRange, saoPaulo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDashboardAsync_CountsALateEveningEntry_OnTheUsersLocalDay_NotTheNextUtcDay()
    {
        // Arrange -- 22:00 on Sep 3 in São Paulo is already 01:00 on Sep 4 in UTC.
        var saoPaulo = TimeSpan.FromHours(-3);
        var tenPmOnTheThird = new DateTimeOffset(2026, 9, 3, 22, 0, 0, saoPaulo);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());

        var dinner = Meal.Create(MealId.NewId(), _userId, tenPmOnTheThird, MealType.Dinner, [CreateItem(600, 30, 60, 20)]);
        _mealRepository.ListByUserAsync(_userId, _dayRange, saoPaulo, Arg.Any<CancellationToken>()).Returns([dinner]);

        var water = WaterEntry.Create(WaterEntryId.NewId(), _userId, tenPmOnTheThird, new Milliliters(300));
        _waterEntryRepository.ListByUserAsync(_userId, _dayRange, saoPaulo, Arg.Any<CancellationToken>()).Returns([water]);

        var weight = WeightEntry.Create(WeightEntryId.NewId(), _userId, tenPmOnTheThird, new Kilograms(87.6));
        _weightEntryRepository.ListByUserAsync(_userId, _dayRange, saoPaulo, Arg.Any<CancellationToken>()).Returns([weight]);

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, saoPaulo);

        // Assert
        var thirdOfSeptember = new DateOnly(2026, 9, 3);
        var day = result!.NutritionHistory.Single(entry => entry.Date == thirdOfSeptember);
        day.Calories.Should().Be(600);
        day.WaterMilliliters.Should().Be(300);
        result.WeightHistory.Should().ContainSingle().Which.Date.Should().Be(thirdOfSeptember);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboardAsync_MapsGoals_FromUserProfile()
    {
        // Arrange
        var user = CreateUser(
            dailyCalorieTarget: 2000,
            targetWeight: new Kilograms(80),
            dailyWaterTarget: new Milliliters(4000),
            dailyProteinTarget: new Grams(140),
            dailyCarbsTarget: new Grams(190),
            dailyFatTarget: new Grams(55));
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert
        result.Should().NotBeNull();
        result!.DailyCalorieTarget.Should().Be(2000);
        result.TargetWeightKilograms.Should().Be(80);
        result.DailyWaterTargetMilliliters.Should().Be(4000);
        result.DailyProteinTargetGrams.Should().Be(140);
        result.DailyCarbsTargetGrams.Should().Be(190);
        result.DailyFatTargetGrams.Should().Be(55);
    }

    [Fact]
    public async Task GetDashboardAsync_MapsGoals_AsNull_WhenUserHasNoOptionalGoalsSet()
    {
        // Arrange
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert
        result.Should().NotBeNull();
        result!.TargetWeightKilograms.Should().BeNull();
        result.DailyWaterTargetMilliliters.Should().BeNull();
        result.DailyProteinTargetGrams.Should().BeNull();
        result.DailyCarbsTargetGrams.Should().BeNull();
        result.DailyFatTargetGrams.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCurrentWeight_FromMostRecentEntry_RegardlessOfRange()
    {
        // Arrange — the most recent entry falls outside [from, to], but it is still "current".
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());
        var mostRecentEntry = WeightEntry.Create(
            WeightEntryId.NewId(),
            _userId,
            new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero),
            new Kilograms(87.6));
        _weightEntryRepository.GetMostRecentAsync(_userId, Arg.Any<CancellationToken>()).Returns(mostRecentEntry);

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert
        result!.CurrentWeightKilograms.Should().Be(87.6);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsWeightHistory_OnlyForDaysWithAnEntry()
    {
        // Arrange — sparse on purpose: no entry logged for 2026-09-02.
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());
        var firstEntry = WeightEntry.Create(
            WeightEntryId.NewId(),
            _userId,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            new Kilograms(88));
        var thirdEntry = WeightEntry.Create(
            WeightEntryId.NewId(),
            _userId,
            new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero),
            new Kilograms(87.6));
        _weightEntryRepository.ListByUserAsync(_userId, _dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([firstEntry, thirdEntry]);

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert
        result!.WeightHistory.Should().HaveCount(2);
        result.WeightHistory.Should().ContainSingle(entry => entry.Date == new DateOnly(2026, 9, 1) && entry.WeightKilograms == 88);
        result.WeightHistory.Should().ContainSingle(entry => entry.Date == new DateOnly(2026, 9, 3) && entry.WeightKilograms == 87.6);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNutritionHistory_WithOneEntryPerDayInRange_ZeroFilled()
    {
        // Arrange — no meals or water logged at all.
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert — one entry per calendar day in [from, to], all zero.
        result!.NutritionHistory.Should().HaveCount(3);
        result.NutritionHistory.Should().OnlyContain(entry =>
            entry.Calories == 0 && entry.Protein == 0 && entry.Carbs == 0 && entry.Fat == 0 && entry.WaterMilliliters == 0);
        result.NutritionHistory.Select(entry => entry.Date).Should().BeEquivalentTo(
            [new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 3)]);
    }

    [Fact]
    public async Task GetDashboardAsync_SumsMultipleMeals_OnTheSameDay_IntoOneNutritionHistoryEntry()
    {
        // Arrange
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateUser());
        var breakfast = CreateMeal(new DateOnly(2026, 9, 1), CreateItem(260, 5.4, 56, 0.6));
        var lunch = CreateMeal(new DateOnly(2026, 9, 1), CreateItem(500, 30, 40, 15));
        _mealRepository.ListByUserAsync(_userId, _dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([breakfast, lunch]);

        var waterEntry = WaterEntry.Create(
            WaterEntryId.NewId(),
            _userId,
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            new Milliliters(500));
        _waterEntryRepository.ListByUserAsync(_userId, _dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([waterEntry]);

        // Act
        var result = await _sut.GetDashboardAsync(_userId, _dayRange, TimeSpan.Zero);

        // Assert
        var firstDay = result!.NutritionHistory.Single(entry => entry.Date == new DateOnly(2026, 9, 1));
        firstDay.Calories.Should().Be(760);
        firstDay.Protein.Should().Be(35.4);
        firstDay.Carbs.Should().Be(96);
        firstDay.Fat.Should().Be(15.6);
        firstDay.WaterMilliliters.Should().Be(500);

        var secondDay = result.NutritionHistory.Single(entry => entry.Date == new DateOnly(2026, 9, 2));
        secondDay.Calories.Should().Be(0);
        secondDay.WaterMilliliters.Should().Be(0);
    }

    private static Meal CreateMeal(DateOnly date, MealItem item) => Meal.Create(
        MealId.NewId(),
        new UserId("firebase-uid-123"),
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
        MealType.Lunch,
        [item]);

    private static MealItem CreateItem(int calories, double protein, double carbs, double fat) => MealItem.Create(
        MealItemId.NewId(),
        FoodId.NewId(),
        200,
        MeasurementUnit.Gram,
        new Kilocalories(calories),
        new Grams(protein),
        new Grams(carbs),
        new Grams(fat));

    private static User CreateUser(
        int dailyCalorieTarget = 2000,
        Kilograms? targetWeight = null,
        Milliliters? dailyWaterTarget = null,
        Grams? dailyProteinTarget = null,
        Grams? dailyCarbsTarget = null,
        Grams? dailyFatTarget = null) => User.Create(
        new UserId("firebase-uid-123"),
        "Victor",
        new Age(27),
        new Centimeters(175),
        Gender.Male,
        ActivityLevel.Moderate,
        new Kilocalories(dailyCalorieTarget),
        new Kilocalories(500),
        targetWeight,
        dailyWaterTarget,
        dailyProteinTarget,
        dailyCarbsTarget,
        dailyFatTarget);
}
