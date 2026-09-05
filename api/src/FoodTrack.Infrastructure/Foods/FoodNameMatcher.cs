namespace FoodTrack.Infrastructure.Foods;

/// <summary>
/// A food name "matches" a query when it contains every word the user typed, case-insensitive,
/// regardless of order or adjacency -- so "arroz cozido" matches "Arroz, integral, cozido" even
/// though "cozido" isn't right after "arroz". Shared by every search-by-name path (the Firestore
/// cache and each external source) so results are consistent no matter where they came from.
/// </summary>
internal static class FoodNameMatcher
{
    private static readonly char[] WordSeparators = [' '];

    public static bool MatchesAllWords(string name, string query)
    {
        var queryWords = query.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return queryWords.All(word => name.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
