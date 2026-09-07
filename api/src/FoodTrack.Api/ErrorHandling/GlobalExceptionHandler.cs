using Microsoft.AspNetCore.Diagnostics;

namespace FoodTrack.Api.ErrorHandling;

/// <summary>
/// Single place that turns an unhandled exception into a structured <see cref="ApiErrorResponse"/>
/// and logs it, replacing the identical try/catch(ArgumentException) block that used to be
/// duplicated in every endpoint's create/update handler.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    private const string UnexpectedErrorMessage = "An unexpected error occurred. Please try again.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, errorCode, message) = MapException(exception);

        _logger.LogError(exception, "Unhandled exception mapped to {StatusCode}/{ErrorCode}.", statusCode, errorCode);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(errorCode, message), cancellationToken);

        return true;
    }

    /// <summary>
    /// Only ArgumentException's message is safe to return as-is -- it's a validation message we
    /// raised ourselves. Every other exception's message may contain internal details (Firestore,
    /// gRPC, HTTP client errors) and is logged above but never sent to the client.
    /// </summary>
    private static (int StatusCode, string ErrorCode, string Message) MapException(Exception exception) => exception switch
    {
        ArgumentException argumentException => (StatusCodes.Status400BadRequest, ValidationErrorCode.InvalidRequest, argumentException.Message),
        _ => (StatusCodes.Status500InternalServerError, GeneralErrorCode.UnexpectedError, UnexpectedErrorMessage),
    };
}
