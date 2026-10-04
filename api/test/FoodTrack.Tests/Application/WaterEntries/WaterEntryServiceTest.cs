using FluentAssertions;
using FoodTrack.Application.WaterEntries;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using FoodTrack.Domain.WaterEntries;
using NSubstitute;

namespace FoodTrack.Tests.Application.WaterEntries;

public sealed class WaterEntryServiceTest
{
    private readonly IWaterEntryRepository _waterEntryRepository = Substitute.For<IWaterEntryRepository>();
    private readonly WaterEntryService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public WaterEntryServiceTest()
    {
        _sut = new WaterEntryService(_waterEntryRepository);
    }

    [Fact]
    public async Task ListAsync_PassesTheUtcOffsetToTheRepository()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 7);
        var saoPaulo = TimeSpan.FromHours(-3);
        _waterEntryRepository.ListByUserAsync(_userId, from, to, saoPaulo, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.ListAsync(_userId, from, to, saoPaulo);

        // Assert
        await _waterEntryRepository.Received(1).ListByUserAsync(_userId, from, to, saoPaulo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Throws_AndSavesNothing_WhenLoggedAtIsInTheFuture()
    {
        // Arrange
        var request = new CreateWaterEntryRequest(DateTimeOffset.UtcNow.AddDays(1), 250);

        // Act
        var act = () => _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _waterEntryRepository.DidNotReceive().AddAsync(Arg.Any<WaterEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 7);
        var loggedAt = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);
        var entry = WaterEntry.Create(WaterEntryId.NewId(), _userId, loggedAt, new Milliliters(250));

        _waterEntryRepository.ListByUserAsync(_userId, from, to, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        // Act
        var result = await _sut.ListAsync(_userId, from, to, TimeSpan.Zero);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(entry.Id.Value);
        result[0].AmountMilliliters.Should().Be(250);
    }

    [Fact]
    public async Task CreateAsync_SavesEntry_AndReturnsDto()
    {
        // Arrange
        var loggedAt = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);
        var request = new CreateWaterEntryRequest(loggedAt, 250);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.LoggedAt.Should().Be(loggedAt);
        result.AmountMilliliters.Should().Be(250);
        await _waterEntryRepository.Received(1).AddAsync(
            Arg.Is<WaterEntry>(entry => entry.UserId == _userId && entry.LoggedAt == loggedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEntryDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = WaterEntryId.NewId();
        _waterEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((WaterEntry?)null);

        // Act
        var result = await _sut.UpdateAsync(
            _userId,
            id,
            new UpdateWaterEntryRequest(new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero), 250));

        // Assert
        result.Should().BeNull();
        await _waterEntryRepository.DidNotReceive().UpdateAsync(Arg.Any<WaterEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SavesEntry_WhenOwnedByCaller()
    {
        // Arrange
        var id = WaterEntryId.NewId();
        var existing = WaterEntry.Create(
            id,
            _userId,
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            new Milliliters(200));
        _waterEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        var newLoggedAt = new DateTimeOffset(2026, 9, 3, 9, 0, 0, TimeSpan.Zero);
        var request = new UpdateWaterEntryRequest(newLoggedAt, 300);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().NotBeNull();
        result!.AmountMilliliters.Should().Be(300);
        await _waterEntryRepository.Received(1).UpdateAsync(
            Arg.Is<WaterEntry>(entry => entry.Id == id && entry.Amount.Value == 300),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = WaterEntryId.NewId();
        _waterEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((WaterEntry?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeFalse();
        await _waterEntryRepository.DidNotReceive().DeleteAsync(Arg.Any<WaterEntryId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenOwnedByCaller()
    {
        // Arrange
        var id = WaterEntryId.NewId();
        var existing = WaterEntry.Create(
            id,
            _userId,
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            new Milliliters(200));
        _waterEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeTrue();
        await _waterEntryRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
