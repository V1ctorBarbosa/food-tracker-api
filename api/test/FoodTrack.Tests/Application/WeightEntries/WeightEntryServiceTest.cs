using FluentAssertions;
using FoodTrack.Application.Common;
using FoodTrack.Application.WeightEntries;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WeightEntries;
using NSubstitute;

namespace FoodTrack.Tests.Application.WeightEntries;

public sealed class WeightEntryServiceTest
{
    private readonly IWeightEntryRepository _weightEntryRepository = Substitute.For<IWeightEntryRepository>();
    private readonly WeightEntryService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public WeightEntryServiceTest()
    {
        _sut = new WeightEntryService(_weightEntryRepository);
    }

    [Fact]
    public async Task ListAsync_PassesTheUtcOffsetToTheRepository()
    {
        // Arrange
        var dayRange = new DayRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7));
        var saoPaulo = TimeSpan.FromHours(-3);
        _weightEntryRepository.ListByUserAsync(_userId, dayRange, saoPaulo, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.ListAsync(_userId, dayRange, saoPaulo);

        // Assert
        await _weightEntryRepository.Received(1).ListByUserAsync(_userId, dayRange, saoPaulo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_UsesTheRequestedLoggedAt_WhenTheEntryIsBackDated()
    {
        // Arrange
        var threeDaysAgo = DateTimeOffset.UtcNow.AddDays(-3);
        var request = new CreateWeightEntryRequest(87.6, threeDaysAgo);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.LoggedAt.Should().Be(threeDaysAgo);
        await _weightEntryRepository.Received(1).AddAsync(
            Arg.Is<WeightEntry>(entry => entry.LoggedAt == threeDaysAgo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Throws_AndSavesNothing_WhenLoggedAtIsInTheFuture()
    {
        // Arrange
        var request = new CreateWeightEntryRequest(87.6, DateTimeOffset.UtcNow.AddDays(1));

        // Act
        var act = () => _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _weightEntryRepository.DidNotReceive().AddAsync(Arg.Any<WeightEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_OrderedByRepository()
    {
        // Arrange
        var dayRange = new DayRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7));
        var loggedAt = new DateTimeOffset(2026, 9, 1, 7, 10, 0, TimeSpan.Zero);
        var entry = WeightEntry.Create(WeightEntryId.NewId(), _userId, loggedAt, new Kilograms(87.6));

        _weightEntryRepository.ListByUserAsync(_userId, dayRange, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        // Act
        var result = await _sut.ListAsync(_userId, dayRange, TimeSpan.Zero);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(entry.Id.Value);
        result[0].WeightKilograms.Should().Be(87.6);
    }

    [Fact]
    public async Task CreateAsync_SavesEntry_AndReturnsDto()
    {
        // Arrange
        var request = new CreateWeightEntryRequest(87.6);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert -- with no LoggedAt requested, the server stamps the current time.
        result.LoggedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.WeightKilograms.Should().Be(87.6);
        await _weightEntryRepository.Received(1).AddAsync(
            Arg.Is<WeightEntry>(entry => entry.UserId == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEntryDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((WeightEntry?)null);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, new UpdateWeightEntryRequest(87));

        // Assert
        result.Should().BeNull();
        await _weightEntryRepository.DidNotReceive().UpdateAsync(Arg.Any<WeightEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SavesEntry_WhenOwnedByCaller()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        var loggedAt = new DateTimeOffset(2026, 9, 1, 7, 10, 0, TimeSpan.Zero);
        var existing = WeightEntry.Create(id, _userId, loggedAt, new Kilograms(88));
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateWeightEntryRequest(87.6);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert -- editing preserves the original LoggedAt, it doesn't reset to the edit time.
        result.Should().NotBeNull();
        result!.WeightKilograms.Should().Be(87.6);
        result.LoggedAt.Should().Be(loggedAt);
        await _weightEntryRepository.Received(1).UpdateAsync(
            Arg.Is<WeightEntry>(entry => entry.Id == id && entry.Weight.Value == 87.6 && entry.LoggedAt == loggedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((WeightEntry?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeFalse();
        await _weightEntryRepository.DidNotReceive().DeleteAsync(Arg.Any<WeightEntryId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenOwnedByCaller()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        var loggedAt = new DateTimeOffset(2026, 9, 1, 7, 10, 0, TimeSpan.Zero);
        var existing = WeightEntry.Create(id, _userId, loggedAt, new Kilograms(88));
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeTrue();
        await _weightEntryRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
