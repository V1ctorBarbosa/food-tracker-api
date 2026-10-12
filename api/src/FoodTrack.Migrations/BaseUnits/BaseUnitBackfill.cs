using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;

namespace FoodTrack.Migrations.BaseUnits;

/// <summary>
/// Gives every cached TACO and Open Food Facts food the base unit its source calls for, and moves
/// the items already logged for a changed food to the same unit. Before this existed every cached
/// food was stored per 100 g, drinks included, and the search never refreshes a cached food.
/// Custom foods are left alone. Running it again changes nothing that is already right.
/// </summary>
internal sealed class BaseUnitBackfill
{
    private readonly IFoodRepository _foodRepository;
    private readonly IMealRepository _mealRepository;
    private readonly IReadOnlyList<IFoodUnitClassifier> _classifiers;

    public BaseUnitBackfill(
        IFoodRepository foodRepository,
        IMealRepository mealRepository,
        IEnumerable<IFoodUnitClassifier> classifiers)
    {
        _foodRepository = foodRepository;
        _mealRepository = mealRepository;
        _classifiers = classifiers.ToList();
    }

    /// <param name="applyChanges">False only reports what would change, writing nothing.</param>
    public async Task<BaseUnitBackfillReport> RunAsync(bool applyChanges, CancellationToken cancellationToken = default)
    {
        var report = new BaseUnitBackfillReport();
        foreach (var classifier in _classifiers)
        {
            await BackfillSourceAsync(classifier, applyChanges, report, cancellationToken);
        }

        return report;
    }

    private async Task BackfillSourceAsync(
        IFoodUnitClassifier classifier,
        bool applyChanges,
        BaseUnitBackfillReport report,
        CancellationToken cancellationToken)
    {
        var foods = await _foodRepository.ListBySourceAsync(classifier.Source, cancellationToken);
        foreach (var food in foods)
        {
            var classifiedUnit = await classifier.ClassifyAsync(food, cancellationToken);
            if (classifiedUnit is not { } unit)
            {
                report.Unclassified.Add(food);
                continue;
            }

            if (unit == food.BaseUnit.Unit)
            {
                report.UnchangedCount++;
                continue;
            }

            report.Changed.Add(new BaseUnitChange(food, unit));
            if (applyChanges)
            {
                await ApplyAsync(food, unit, cancellationToken);
            }
        }
    }

    // Items move first: if saving the food then fails, the next run still sees the food as
    // unchanged and finishes the job, while the reverse order would leave items it never revisits.
    private async Task ApplyAsync(Food food, MeasurementUnit unit, CancellationToken cancellationToken)
    {
        await _mealRepository.ReclassifyItemUnitsAsync(food.Id, unit.GetCategory(), cancellationToken);
        await _foodRepository.SaveAsync(food.MeasuredIn(unit), cancellationToken);
    }
}
