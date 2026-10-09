using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Migrations.BaseUnits;

namespace FoodTrack.Tests.Migrations.BaseUnits;

public sealed class BaseUnitBackfillReportFormatterTest
{
    [Fact]
    public void Format_ListsEachChangeAndSkip_AndEndsWithTheCounts()
    {
        // Arrange
        var report = new BaseUnitBackfillReport { UnchangedCount = 3 };
        report.Changed.Add(new BaseUnitChange(CreateFood("Coca-Cola", "7894900011708"), MeasurementUnit.Milliliter));
        report.Unclassified.Add(CreateFood("Suco descontinuado", "0000000000000"));

        // Act
        var text = BaseUnitBackfillReportFormatter.Format(report, changesWereApplied: true);

        // Assert
        text.Should().StartWith("Applied:");
        text.Should().Contain("OpenFoodFacts 7894900011708 \"Coca-Cola\": Gram -> Milliliter");
        text.Should().Contain("skipped, no answer from OpenFoodFacts: 0000000000000 \"Suco descontinuado\"");
        text.Should().EndWith("1 changed, 3 already right, 1 skipped.");
    }

    [Fact]
    public void Format_SaysNothingWasWritten_OnADryRun()
    {
        // Act
        var text = BaseUnitBackfillReportFormatter.Format(new BaseUnitBackfillReport(), changesWereApplied: false);

        // Assert
        text.Should().StartWith("Dry run, nothing written");
    }

    private static Food CreateFood(string name, string externalId) => Food.Create(
        FoodId.NewId(),
        name,
        FoodSource.OpenFoodFacts,
        externalId,
        new BaseUnit(100, MeasurementUnit.Gram),
        new Kilocalories(42),
        new Grams(0),
        new Grams(10.5),
        new Grams(0));
}
