using System.Text;

namespace FoodTrack.Migrations.BaseUnits;

internal static class BaseUnitBackfillReportFormatter
{
    public static string Format(BaseUnitBackfillReport report, bool changesWereApplied)
    {
        var text = new StringBuilder();
        text.AppendLine(DescribeMode(changesWereApplied));

        foreach (var change in report.Changed)
        {
            text.AppendLine($"  {change.Food.Source} {change.Food.ExternalId} \"{change.Food.Name}\": {change.Food.BaseUnit.Unit} -> {change.NewUnit}");
        }

        foreach (var food in report.Unclassified)
        {
            text.AppendLine($"  skipped, no answer from {food.Source}: {food.ExternalId} \"{food.Name}\"");
        }

        text.Append($"{report.Changed.Count} changed, {report.UnchangedCount} already right, {report.Unclassified.Count} skipped.");
        return text.ToString();
    }

    private static string DescribeMode(bool changesWereApplied)
    {
        if (changesWereApplied)
        {
            return "Applied:";
        }

        return "Dry run, nothing written (pass --Apply true to write):";
    }
}
