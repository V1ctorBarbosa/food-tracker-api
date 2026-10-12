using System.Text.Json;
using FluentAssertions;
using FoodTrack.Api.ErrorHandling;
using FoodTrack.Application.MealEstimates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FoodTrack.Tests.Api.ErrorHandling;

public sealed class GlobalExceptionHandlerTest
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTest()
    {
        _sut = new GlobalExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_WritesBadRequest_ForArgumentException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new ArgumentException("Quantity must be positive.");

        // Act
        var wasHandled = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        wasHandled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(ValidationErrorCode.InvalidRequest);
        body.Message.Should().Be("Quantity must be positive.");
    }

    [Fact]
    public async Task TryHandleAsync_WritesInternalServerError_ForAnyOtherException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Something unexpected happened.");

        // Act
        var wasHandled = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        wasHandled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(GeneralErrorCode.UnexpectedError);
        body.Message.Should().NotBe(exception.Message);
    }

    [Fact]
    public async Task TryHandleAsync_WritesForbidden_WhenTheUserMayNotEstimateMeals()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        // Act
        await _sut.TryHandleAsync(httpContext, new MealEstimateAccessDeniedException(), CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(MealEstimateErrorCode.AccessDenied);
    }

    [Fact]
    public async Task TryHandleAsync_WritesUnprocessableEntity_WhenNoFoodIsRecognized()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        // Act
        await _sut.TryHandleAsync(httpContext, new NoFoodRecognizedException(), CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(MealEstimateErrorCode.NoFoodRecognized);
    }

    [Fact]
    public async Task TryHandleAsync_WritesBadGateway_WithoutTheEstimatorDetails_WhenTheEstimatorIsUnavailable()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new MealEstimatorUnavailableException("The estimator returned an error.", new InvalidOperationException("internal detail"));

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(MealEstimateErrorCode.EstimateUnavailable);
        body.Message.Should().NotContain("internal detail");
    }

    private static DefaultHttpContext CreateHttpContext() => new() { Response = { Body = new MemoryStream() } };

    private static async Task<ApiErrorResponse> ReadResponseBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return body!;
    }
}
