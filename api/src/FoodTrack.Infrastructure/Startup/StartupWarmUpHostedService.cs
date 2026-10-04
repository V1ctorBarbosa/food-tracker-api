using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodTrack.Infrastructure.Startup;

/// <summary>
/// Runs every <see cref="IStartupWarmUp"/> while the host starts. A warm-up is an optimisation, so
/// a failure or a slow one is logged and skipped: it must never keep the host from serving requests.
/// </summary>
public sealed class StartupWarmUpHostedService : IHostedService
{
    private static readonly TimeSpan DefaultWarmUpTimeout = TimeSpan.FromSeconds(8);

    private readonly IReadOnlyList<IStartupWarmUp> _warmUps;
    private readonly ILogger<StartupWarmUpHostedService> _logger;
    private readonly TimeSpan _warmUpTimeout;

    public StartupWarmUpHostedService(IEnumerable<IStartupWarmUp> warmUps, ILogger<StartupWarmUpHostedService> logger)
        : this(warmUps, logger, DefaultWarmUpTimeout)
    {
    }

    internal StartupWarmUpHostedService(IEnumerable<IStartupWarmUp> warmUps, ILogger<StartupWarmUpHostedService> logger, TimeSpan warmUpTimeout)
    {
        _warmUps = warmUps.ToList();
        _logger = logger;
        _warmUpTimeout = warmUpTimeout;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_warmUpTimeout);

        await Task.WhenAll(_warmUps.Select(warmUp => RunSafelyAsync(warmUp, timeoutSource.Token)));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task RunSafelyAsync(IStartupWarmUp warmUp, CancellationToken cancellationToken)
    {
        try
        {
            await warmUp.WarmUpAsync(cancellationToken);
            _logger.LogInformation("Startup warm-up '{WarmUpName}' finished.", warmUp.Name);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Startup warm-up '{WarmUpName}' was skipped.", warmUp.Name);
        }
    }
}
