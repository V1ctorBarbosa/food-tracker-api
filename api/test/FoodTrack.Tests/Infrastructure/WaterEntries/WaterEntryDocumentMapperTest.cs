using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;
using FoodTrack.Infrastructure.WaterEntries;

namespace FoodTrack.Tests.Infrastructure.WaterEntries;

public sealed class WaterEntryDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromWaterEntry()
    {
        // Arrange
        var loggedAt = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);
        var entry = WaterEntry.Create(
            WaterEntryId.NewId(),
            new UserId("firebase-uid-123"),
            loggedAt,
            new Milliliters(250));

        // Act
        var document = WaterEntryDocumentMapper.ToDocument(entry);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.LoggedAt.Should().Be(loggedAt);
        document.AmountMilliliters.Should().Be(250);
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = WaterEntryId.NewId();
        var originalEntry = WaterEntry.Create(
            id,
            new UserId("firebase-uid-123"),
            new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero),
            new Milliliters(250));

        // Act
        var document = WaterEntryDocumentMapper.ToDocument(originalEntry);
        var roundTrippedEntry = WaterEntryDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedEntry.Id.Should().Be(id);
        roundTrippedEntry.UserId.Should().Be(originalEntry.UserId);
        roundTrippedEntry.LoggedAt.Should().Be(originalEntry.LoggedAt);
        roundTrippedEntry.Amount.Should().Be(originalEntry.Amount);
    }
}
