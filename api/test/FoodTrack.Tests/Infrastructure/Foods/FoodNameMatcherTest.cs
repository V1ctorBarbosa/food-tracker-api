using FluentAssertions;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Tests.Infrastructure.Foods;

public sealed class FoodNameMatcherTest
{
    [Theory]
    [InlineData("Arroz, integral, cozido", "arroz cozido", true)]
    [InlineData("Arroz, integral, cru", "arroz cozido", false)]
    [InlineData("Arroz, integral, cozido", "ARROZ COZIDO", true)]
    [InlineData("Feijão, carioca, cozido", "arroz", false)]
    [InlineData("Arroz, integral, cozido", "integral", true)]
    [InlineData("Arroz, integral, cozido", "  arroz   cozido  ", true)]
    [InlineData("Arroz, integral, cozido", "cozido arroz", true)]
    [InlineData("Arroz, integral, cozido", "", true)]
    public void MatchesAllWords_ReturnsExpected(string name, string query, bool expected)
    {
        FoodNameMatcher.MatchesAllWords(name, query).Should().Be(expected);
    }
}
