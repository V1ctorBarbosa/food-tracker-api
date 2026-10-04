using FluentAssertions;
using FoodTrack.Infrastructure.Startup;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FoodTrack.Tests.Infrastructure.Startup;

public sealed class StartupWarmUpHostedServiceTest
{
    private static readonly TimeSpan GenerousTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task StartAsync_RunsEveryWarmUp()
    {
        // Arrange
        var firstWarmUp = CreateWarmUp("first");
        var secondWarmUp = CreateWarmUp("second");
        var service = CreateService(GenerousTimeout, firstWarmUp, secondWarmUp);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        await firstWarmUp.Received(1).WarmUpAsync(Arg.Any<CancellationToken>());
        await secondWarmUp.Received(1).WarmUpAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenAWarmUpFails()
    {
        // Arrange
        var failingWarmUp = CreateWarmUp("failing");
        failingWarmUp.WarmUpAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("boom")));
        var service = CreateService(GenerousTimeout, failingWarmUp);

        // Act
        var act = () => service.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_StillRunsTheOtherWarmUps_WhenOneFails()
    {
        // Arrange
        var failingWarmUp = CreateWarmUp("failing");
        failingWarmUp.WarmUpAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("boom")));
        var healthyWarmUp = CreateWarmUp("healthy");
        var service = CreateService(GenerousTimeout, failingWarmUp, healthyWarmUp);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        await healthyWarmUp.Received(1).WarmUpAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_CancelsAWarmUpThatOutlastsTheTimeout()
    {
        // Arrange
        var wasCancelled = false;
        var slowWarmUp = CreateWarmUp("slow");
        slowWarmUp.WarmUpAsync(Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, callInfo.Arg<CancellationToken>());
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                throw;
            }
        });
        var service = CreateService(ShortTimeout, slowWarmUp);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        wasCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_WithNoWarmUps_Completes()
    {
        // Arrange
        var service = CreateService(GenerousTimeout);

        // Act
        var act = () => service.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        // Arrange
        var service = CreateService(GenerousTimeout);

        // Act
        var act = () => service.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static IStartupWarmUp CreateWarmUp(string name)
    {
        var warmUp = Substitute.For<IStartupWarmUp>();
        warmUp.Name.Returns(name);
        warmUp.WarmUpAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return warmUp;
    }

    private static StartupWarmUpHostedService CreateService(TimeSpan timeout, params IStartupWarmUp[] warmUps)
    {
        return new StartupWarmUpHostedService(warmUps, NullLogger<StartupWarmUpHostedService>.Instance, timeout);
    }
}
