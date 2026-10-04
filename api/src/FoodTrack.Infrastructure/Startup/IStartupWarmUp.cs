namespace FoodTrack.Infrastructure.Startup;

/// <summary>
/// One piece of work that is expensive the first time it runs (opening a connection, loading keys)
/// and is cheaper to pay for while the host starts than during the first user request.
/// </summary>
public interface IStartupWarmUp
{
    string Name { get; }

    Task WarmUpAsync(CancellationToken cancellationToken);
}
