using FluentAssertions;
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
    public async Task ListAsync_ReturnsMappedDtos_OrderedByRepository()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 7);
        var entry = WeightEntry.Create(WeightEntryId.NewId(), _userId, from, new Kilograms(87.6));

        _weightEntryRepository.ListByUserAsync(_userId, from, to, Arg.Any<CancellationToken>())
            .Returns([entry]);

        // Act
        var result = await _sut.ListAsync(_userId, from, to);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(entry.Id.Value);
        result[0].WeightKilograms.Should().Be(87.6);
    }

    [Fact]
    public async Task CreateAsync_SavesEntry_AndReturnsDto()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 3);
        var request = new CreateWeightEntryRequest(date, 87.6);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Date.Should().Be(date);
        result.WeightKilograms.Should().Be(87.6);
        await _weightEntryRepository.Received(1).AddAsync(
            Arg.Is<WeightEntry>(entry => entry.UserId == _userId && entry.Date == date),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEntryDoesNotExistOrIsNotOwned()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((WeightEntry?)null);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, new UpdateWeightEntryRequest(new DateOnly(2026, 9, 3), 87));

        // Assert
        result.Should().BeNull();
        await _weightEntryRepository.DidNotReceive().UpdateAsync(Arg.Any<WeightEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SavesEntry_WhenOwnedByCaller()
    {
        // Arrange
        var id = WeightEntryId.NewId();
        var existing = WeightEntry.Create(id, _userId, new DateOnly(2026, 9, 1), new Kilograms(88));
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateWeightEntryRequest(new DateOnly(2026, 9, 3), 87.6);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().NotBeNull();
        result!.WeightKilograms.Should().Be(87.6);
        await _weightEntryRepository.Received(1).UpdateAsync(
            Arg.Is<WeightEntry>(entry => entry.Id == id && entry.Weight.Value == 87.6),
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
        var existing = WeightEntry.Create(id, _userId, new DateOnly(2026, 9, 1), new Kilograms(88));
        _weightEntryRepository.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeTrue();
        await _weightEntryRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
