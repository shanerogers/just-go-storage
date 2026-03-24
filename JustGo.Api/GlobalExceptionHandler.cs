using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Api;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is JustGoApiException apiEx)
        {
            logger.LogWarning("JustGo API error {StatusCode}: {Body}", apiEx.StatusCode, apiEx.Body);

            var statusCode = apiEx.StatusCode switch
            {
                400 => StatusCodes.Status400BadRequest,
                401 => StatusCodes.Status401Unauthorized,
                403 => StatusCodes.Status403Forbidden,
                404 => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status502BadGateway
            };

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = "JustGo API Error",
                Detail = apiEx.Body
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception");
        var fallbackProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server Error",
            Detail = "An unexpected error occurred."
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(fallbackProblem, cancellationToken);
        return true;
    }
}
