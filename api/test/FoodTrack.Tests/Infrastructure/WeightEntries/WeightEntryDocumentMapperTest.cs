using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;
using FoodTrack.Infrastructure.WeightEntries;

namespace FoodTrack.Tests.Infrastructure.WeightEntries;

public sealed class WeightEntryDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromWeightEntry()
    {
        // Arrange
        var loggedAt = new DateTimeOffset(2026, 9, 3, 7, 10, 0, TimeSpan.Zero);
        var entry = WeightEntry.Create(
            WeightEntryId.NewId(),
            new UserId("firebase-uid-123"),
            loggedAt,
            new Kilograms(87.6));

        // Act
        var document = WeightEntryDocumentMapper.ToDocument(entry);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.LoggedAt.Should().Be(loggedAt);
        document.WeightKilograms.Should().Be(87.6);
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        var loggedAt = new DateTimeOffset(2026, 9, 3, 7, 10, 0, TimeSpan.Zero);
        var originalEntry = WeightEntry.Create(
            id,
            new UserId("firebase-uid-123"),
            loggedAt,
            new Kilograms(87.6));

        // Act
        var document = WeightEntryDocumentMapper.ToDocument(originalEntry);
        var roundTrippedEntry = WeightEntryDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedEntry.Id.Should().Be(id);
        roundTrippedEntry.UserId.Should().Be(originalEntry.UserId);
        roundTrippedEntry.LoggedAt.Should().Be(originalEntry.LoggedAt);
        roundTrippedEntry.Weight.Should().Be(originalEntry.Weight);
    }
}
