using FluentAssertions;
using FoodTrack.Application.Common;

namespace FoodTrack.Tests.Application.Common;

public sealed class LocalDayRangeTest
{
    private static readonly TimeSpan SaoPaulo = TimeSpan.FromHours(-3);

    [Fact]
    public void ToInstants_AtUtc_CoversTheWholeUtcDay()
    {
        var day = new DateOnly(2026, 9, 3);

        var (fromInclusive, toExclusive) = LocalDayRange.ToInstants(new DayRange(day, day), TimeSpan.Zero);

        fromInclusive.Should().Be(new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero));
        toExclusive.Should().Be(new DateTimeOffset(2026, 9, 4, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToInstants_WithANegativeOffset_StartsAtTheUsersMidnight()
    {
        var day = new DateOnly(2026, 9, 3);

        var (fromInclusive, toExclusive) = LocalDayRange.ToInstants(new DayRange(day, day), SaoPaulo);

        fromInclusive.UtcDateTime.Should().Be(new DateTime(2026, 9, 3, 3, 0, 0, DateTimeKind.Utc));
        toExclusive.UtcDateTime.Should().Be(new DateTime(2026, 9, 4, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToInstants_AcrossSeveralDays_EndsAfterTheLastDay()
    {
        var (fromInclusive, toExclusive) = LocalDayRange.ToInstants(new DayRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7)), SaoPaulo);

        (toExclusive - fromInclusive).Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public void ToInstants_PutsALateEveningEntryInsideThatEveningsDay()
    {
        var day = new DateOnly(2026, 9, 3);
        var tenPmInSaoPaulo = new DateTimeOffset(2026, 9, 3, 22, 0, 0, SaoPaulo);

        var (fromInclusive, toExclusive) = LocalDayRange.ToInstants(new DayRange(day, day), SaoPaulo);

        tenPmInSaoPaulo.Should().BeOnOrAfter(fromInclusive).And.BeBefore(toExclusive);
    }

    [Fact]
    public void DateOf_UsesTheUsersOffset_NotUtc()
    {
        var oneAmUtcOnTheFourth = new DateTimeOffset(2026, 9, 4, 1, 0, 0, TimeSpan.Zero);

        var result = LocalDayRange.DateOf(oneAmUtcOnTheFourth, SaoPaulo);

        result.Should().Be(new DateOnly(2026, 9, 3));
    }

    [Fact]
    public void DateOf_AtUtcOffsetZero_ReturnsTheUtcDate()
    {
        var instant = new DateTimeOffset(2026, 9, 4, 1, 0, 0, TimeSpan.Zero);

        var result = LocalDayRange.DateOf(instant, TimeSpan.Zero);

        result.Should().Be(new DateOnly(2026, 9, 4));
    }
}
