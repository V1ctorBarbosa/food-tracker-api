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
        var entry = WeightEntry.Create(
            WeightEntryId.NewId(),
            new UserId("firebase-uid-123"),
            new DateOnly(2026, 9, 3),
            new Kilograms(87.6));

        // Act
        var document = WeightEntryDocumentMapper.ToDocument(entry);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.Date.Should().Be(new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));
        document.WeightKilograms.Should().Be(87.6);
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        var originalEntry = WeightEntry.Create(
            id,
            new UserId("firebase-uid-123"),
            new DateOnly(2026, 9, 3),
            new Kilograms(87.6));

        // Act
        var document = WeightEntryDocumentMapper.ToDocument(originalEntry);
        var roundTrippedEntry = WeightEntryDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedEntry.Id.Should().Be(id);
        roundTrippedEntry.UserId.Should().Be(originalEntry.UserId);
        roundTrippedEntry.Date.Should().Be(originalEntry.Date);
        roundTrippedEntry.Weight.Should().Be(originalEntry.Weight);
    }
}
