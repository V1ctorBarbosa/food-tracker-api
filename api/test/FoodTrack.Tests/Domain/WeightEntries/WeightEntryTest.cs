using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;

namespace FoodTrack.Tests.Domain.WeightEntries;

public sealed class WeightEntryTest
{
    [Fact]
    public void Create_ReturnsWeightEntry_WithGivenValues()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        var userId = new UserId("firebase-uid-123");
        var date = new DateOnly(2026, 9, 3);
        var weight = new Kilograms(87.6);

        // Act
        var entry = WeightEntry.Create(id, userId, date, weight);

        // Assert
        entry.Id.Should().Be(id);
        entry.UserId.Should().Be(userId);
        entry.Date.Should().Be(date);
        entry.Weight.Should().Be(weight);
    }
}
