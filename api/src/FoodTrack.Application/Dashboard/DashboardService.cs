using FoodTrack.Application.Common;
using FoodTrack.Application.Meals;
using FoodTrack.Application.Users;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Application.Dashboard;

public sealed class DashboardService
{
    private readonly IUserRepository _userRepository;
    private readonly IMealRepository _mealRepository;
    private readonly IWaterEntryRepository _waterEntryRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;

    public DashboardService(
        IUserRepository userRepository,
        IMealRepository mealRepository,
        IWaterEntryRepository waterEntryRepository,
        IWeightEntryRepository weightEntryRepository)
    {
        _userRepository = userRepository;
        _mealRepository = mealRepository;
        _waterEntryRepository = waterEntryRepository;
        _weightEntryRepository = weightEntryRepository;
    }

    public async Task<DashboardDto?> GetDashboardAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var meals = await _mealRepository.ListSummariesByUserAsync(userId, from, to, utcOffset, cancellationToken);
        var waterEntries = await _waterEntryRepository.ListByUserAsync(userId, from, to, utcOffset, cancellationToken);
        var weightEntries = await _weightEntryRepository.ListByUserAsync(userId, from, to, utcOffset, cancellationToken);
        var mostRecentWeightEntry = await _weightEntryRepository.GetMostRecentAsync(userId, cancellationToken);

        var weightHistory = weightEntries
            .Select(entry => new WeightHistoryEntryDto(LocalDayRange.DateOf(entry.LoggedAt, utcOffset), entry.Weight.Value))
            .ToList();

        return new DashboardDto(
            user.DailyCalorieTarget.Value,
            mostRecentWeightEntry?.Weight.Value,
            user.TargetWeight?.Value,
            user.DailyWaterTarget?.Value,
            user.DailyProteinTarget?.Value,
            user.DailyCarbsTarget?.Value,
            user.DailyFatTarget?.Value,
            weightHistory,
            BuildNutritionHistory(from, to, utcOffset, meals, waterEntries));
    }

    private static List<NutritionHistoryEntryDto> BuildNutritionHistory(
        DateOnly from,
        DateOnly to,
        TimeSpan utcOffset,
        IReadOnlyList<MealSummary> meals,
        IReadOnlyList<WaterEntry> waterEntries)
    {
        var mealTotalsByDate = meals
            .GroupBy(meal => LocalDayRange.DateOf(meal.LoggedAt, utcOffset))
            .ToDictionary(
                group => group.Key,
                group => (
                    Calories: group.Sum(meal => meal.TotalCalories.Value),
                    Protein: group.Sum(meal => meal.TotalProtein.Value),
                    Carbs: group.Sum(meal => meal.TotalCarbs.Value),
                    Fat: group.Sum(meal => meal.TotalFat.Value)));

        var waterTotalByDate = waterEntries
            .GroupBy(entry => LocalDayRange.DateOf(entry.LoggedAt, utcOffset))
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Amount.Value));

        var nutritionHistory = new List<NutritionHistoryEntryDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var mealTotals = mealTotalsByDate.GetValueOrDefault(date);
            nutritionHistory.Add(new NutritionHistoryEntryDto(
                date,
                mealTotals.Calories,
                mealTotals.Protein,
                mealTotals.Carbs,
                mealTotals.Fat,
                waterTotalByDate.GetValueOrDefault(date)));
        }

        return nutritionHistory;
    }
}
