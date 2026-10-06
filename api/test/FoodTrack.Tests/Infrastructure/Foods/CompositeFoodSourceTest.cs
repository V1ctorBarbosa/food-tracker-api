using System.Net;
using System.Text;
using FluentAssertions;
using FoodTrack.Infrastructure.Foods;

namespace FoodTrack.Tests.Infrastructure.Foods;

public sealed class CompositeFoodSourceTest
{
    private const string TacoFoodListJson = """{"foods":[{"id":1,"description":"Arroz, tipo 1, cozido"}]}""";
    private const string TacoFoodDetailJson =
        """{"id":1,"description":"Arroz, tipo 1, cozido","energy_kcal":128,"protein_g":2.5,"lipids_g":0.2,"carbohydrate_g":28.1}""";

    [Fact]
    public async Task SearchAsync_ReturnsTheOtherSourcesResults_WhenOneSourceTimesOut()
    {
        // Arrange
        var compositeFoodSource = new CompositeFoodSource(
            new TacoFoodDataClient(CreateHttpClient("https://taco.test/", RespondWithTacoFoods)),
            new OpenFoodFactsClient(CreateHttpClient("https://openfoodfacts.test/", _ => throw new TaskCanceledException())));

        // Act
        var foods = await compositeFoodSource.SearchAsync("arroz cozido");

        // Assert
        foods.Should().ContainSingle().Which.Name.Should().Be("Arroz, tipo 1, cozido");
    }

    [Fact]
    public async Task SearchAsync_Throws_WhenTheCallerCancels()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var compositeFoodSource = new CompositeFoodSource(
            new TacoFoodDataClient(CreateHttpClient("https://taco.test/", RespondWithTacoFoods)),
            new OpenFoodFactsClient(CreateHttpClient("https://openfoodfacts.test/", RespondWithTacoFoods)));

        // Act
        var act = () => compositeFoodSource.SearchAsync("arroz cozido", cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HttpResponseMessage RespondWithTacoFoods(HttpRequestMessage request)
    {
        var isFoodList = request.RequestUri!.AbsolutePath.EndsWith("/foods.json", StringComparison.Ordinal);
        var responseJson = TacoFoodDetailJson;
        if (isFoodList)
        {
            responseJson = TacoFoodListJson;
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpClient CreateHttpClient(string baseUrl, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        return new HttpClient(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri(baseUrl) };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_respond(request));
        }
    }
}
