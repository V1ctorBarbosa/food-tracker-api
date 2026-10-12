namespace FoodTrack.Migrations.BaseUnits;

/// <summary>How fast the migration may call Open Food Facts, and how it backs off when told to slow down.</summary>
/// <param name="IntervalBetweenRequests">Pause before every product request.</param>
/// <param name="WaitAfterRateLimit">Pause after a 429 that does not say how long to wait.</param>
/// <param name="MaximumAttempts">Requests made for one product before giving up on it.</param>
internal sealed record OpenFoodFactsRequestPacing(TimeSpan IntervalBetweenRequests, TimeSpan WaitAfterRateLimit, int MaximumAttempts)
{
    // Open Food Facts documents 100 product reads per minute, but answered 429 well before that at
    // 0.7 s apart; one second apart keeps a full run under it with margin.
    public static readonly OpenFoodFactsRequestPacing Default = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60), 5);
}
