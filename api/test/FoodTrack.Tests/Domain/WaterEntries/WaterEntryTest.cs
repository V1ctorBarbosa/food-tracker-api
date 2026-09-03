using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;

namespace FoodTrack.Tests.Domain.WaterEntries;

public sealed class WaterEntryTest
{
    [Fact]
    public void Create_ReturnsWaterEntry_WithGivenValues()
    {
        // Arrange
        var id = WaterEntryId.NewId();
        var userId = new UserId("firebase-uid-123");
        var loggedAt = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);
        var amount = new Milliliters(250);

        // Act
        var entry = WaterEntry.Create(id, userId, loggedAt, amount);

        // Assert
        entry.Id.Should().Be(id);
        entry.UserId.Should().Be(userId);
        entry.LoggedAt.Should().Be(loggedAt);
        entry.Amount.Should().Be(amount);
    }
}
