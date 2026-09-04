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

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, errorCode) = MapException(exception);

        _logger.LogError(exception, "Unhandled exception mapped to {StatusCode}/{ErrorCode}.", statusCode, errorCode);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(errorCode, exception.Message), cancellationToken);

        return true;
    }

    private static (int StatusCode, string ErrorCode) MapException(Exception exception) => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, ValidationErrorCode.InvalidRequest),
        _ => (StatusCodes.Status500InternalServerError, GeneralErrorCode.UnexpectedError),
    };
}
