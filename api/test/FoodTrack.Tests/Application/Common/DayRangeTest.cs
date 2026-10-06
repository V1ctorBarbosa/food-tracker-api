using System.Globalization;
using FluentAssertions;
using FoodTrack.Application.Common;

namespace FoodTrack.Tests.Application.Common;

public sealed class DayRangeTest
{
    [Theory]
    [InlineData("2026-09-03", "2026-09-03")]
    [InlineData("2026-09-01", "2026-09-30")]
    [InlineData("2024-01-01", "2024-12-31")]
    public void Constructor_KeepsTheDays_WhenTheRangeIsWithinTheLimit(string from, string to)
    {
        // Arrange
        var fromDay = DateOnly.Parse(from, CultureInfo.InvariantCulture);
        var toDay = DateOnly.Parse(to, CultureInfo.InvariantCulture);

        // Act
        var dayRange = new DayRange(fromDay, toDay);

        // Assert
        dayRange.From.Should().Be(fromDay);
        dayRange.To.Should().Be(toDay);
    }

    [Fact]
    public void Constructor_Throws_WhenFromIsAfterTo()
    {
        // Act
        var act = () => new DayRange(new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 3));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("2024-01-01", "2025-01-01")]
    [InlineData("0001-01-01", "9999-12-31")]
    public void Constructor_Throws_WhenTheRangeSpansMoreThanTheMaximumDayCount(string from, string to)
    {
        // Act
        var act = () => new DayRange(DateOnly.Parse(from, CultureInfo.InvariantCulture), DateOnly.Parse(to, CultureInfo.InvariantCulture));

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
