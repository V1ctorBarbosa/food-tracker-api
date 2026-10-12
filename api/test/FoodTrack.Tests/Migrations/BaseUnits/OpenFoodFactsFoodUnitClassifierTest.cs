using System.Net;
using System.Text;
using FluentAssertions;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Foods;
using FoodTrack.Migrations.BaseUnits;

namespace FoodTrack.Tests.Migrations.BaseUnits;

public sealed class OpenFoodFactsFoodUnitClassifierTest
{
    private const string DrinkProductJson =
        """{"status":1,"product":{"quantity":"350ml","categories_tags":["en:beverages"]}}""";

    private static readonly OpenFoodFactsRequestPacing NoWaiting = new(TimeSpan.Zero, TimeSpan.Zero, MaximumAttempts: 3);

    [Fact]
    public async Task ClassifyAsync_RetriesAfterARateLimit_AndClassifiesTheProduct()
    {
        // Arrange: Open Food Facts answered 429 to most of a real run before letting requests through.
        var handler = new ScriptedHandler(
            Respond(HttpStatusCode.TooManyRequests),
            Respond(HttpStatusCode.TooManyRequests),
            Respond(HttpStatusCode.OK, DrinkProductJson));
        var sut = CreateClassifier(handler);

        // Act
        var unit = await sut.ClassifyAsync(CreateFood("7891149440801"));

        // Assert
        unit.Should().Be(MeasurementUnit.Milliliter);
        handler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task ClassifyAsync_GivesUp_AfterTheMaximumAttempts()
    {
        // Arrange
        var handler = new ScriptedHandler(
            Respond(HttpStatusCode.TooManyRequests),
            Respond(HttpStatusCode.TooManyRequests),
            Respond(HttpStatusCode.TooManyRequests),
            Respond(HttpStatusCode.OK, DrinkProductJson));
        var sut = CreateClassifier(handler);

        // Act
        var unit = await sut.ClassifyAsync(CreateFood("7891149440801"));

        // Assert
        unit.Should().BeNull();
        handler.RequestCount.Should().Be(NoWaiting.MaximumAttempts);
    }

    [Fact]
    public async Task ClassifyAsync_DoesNotRetry_AProductThatNoLongerExists()
    {
        // Arrange
        var handler = new ScriptedHandler(Respond(HttpStatusCode.NotFound), Respond(HttpStatusCode.OK, DrinkProductJson));
        var sut = CreateClassifier(handler);

        // Act
        var unit = await sut.ClassifyAsync(CreateFood("7896096502343"));

        // Assert
        unit.Should().BeNull();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ClassifyAsync_ReturnsNull_WhenOpenFoodFactsSaysTheProductWasNotFound()
    {
        // Arrange
        var handler = new ScriptedHandler(Respond(HttpStatusCode.OK, """{"status":0}"""));
        var sut = CreateClassifier(handler);

        // Act
        var unit = await sut.ClassifyAsync(CreateFood("0000000000000"));

        // Assert
        unit.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_ReturnsNull_WithoutCallingOpenFoodFacts_WhenTheFoodHasNoBarcode()
    {
        // Arrange
        var handler = new ScriptedHandler(Respond(HttpStatusCode.OK, DrinkProductJson));
        var sut = CreateClassifier(handler);

        // Act
        var unit = await sut.ClassifyAsync(CreateFood(externalId: null));

        // Assert
        unit.Should().BeNull();
        handler.RequestCount.Should().Be(0);
    }

    private static OpenFoodFactsFoodUnitClassifier CreateClassifier(ScriptedHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://world.openfoodfacts.org/") }, NoWaiting);

    private static HttpResponseMessage Respond(HttpStatusCode statusCode, string body = "{}") => new(statusCode)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private static Food CreateFood(string? externalId) => Food.Create(
        FoodId.NewId(),
        "Refrigerante",
        FoodSource.OpenFoodFacts,
        externalId,
        new BaseUnit(100, MeasurementUnit.Gram),
        new Kilocalories(42),
        new Grams(0),
        new Grams(10.5),
        new Grams(0));

    /// <summary>Answers each request with the next scripted response, in order.</summary>
    private sealed class ScriptedHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
