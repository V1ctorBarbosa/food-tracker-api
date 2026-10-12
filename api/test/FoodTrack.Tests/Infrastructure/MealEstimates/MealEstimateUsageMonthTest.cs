using FluentAssertions;
using FoodTrack.Infrastructure.MealEstimates;

namespace FoodTrack.Tests.Infrastructure.MealEstimates;

public sealed class MealEstimateUsageMonthTest
{
    [Fact]
    public void KeyFor_ReturnsTheYearAndMonth()
    {
        // Act
        var key = MealEstimateUsageMonth.KeyFor(new DateTimeOffset(2026, 10, 12, 15, 0, 0, TimeSpan.Zero));

        // Assert
        key.Should().Be("2026-10");
    }

    [Fact]
    public void KeyFor_UsesTheUtcMonth_WhenTheLocalDayIsStillTheMonthBefore()
    {
        // Arrange: 22:30 on October 31 in Brasília is already November 1 in UTC.
        var lateOctoberInBrasilia = new DateTimeOffset(2026, 10, 31, 22, 30, 0, TimeSpan.FromHours(-3));

        // Act
        var key = MealEstimateUsageMonth.KeyFor(lateOctoberInBrasilia);

        // Assert
        key.Should().Be("2026-11");
    }

    [Theory]
    [InlineData(0, 300, true)]
    [InlineData(299, 300, true)]
    [InlineData(300, 300, false)]
    [InlineData(301, 300, false)]
    public void HasRoomFor_AllowsEstimatesOnlyBelowTheLimit(int usedCount, int monthlyLimit, bool expectedHasRoom)
    {
        // Act
        var hasRoom = MealEstimateUsageMonth.HasRoomFor(usedCount, monthlyLimit);

        // Assert
        hasRoom.Should().Be(expectedHasRoom);
    }
}
