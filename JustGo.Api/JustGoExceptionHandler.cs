using System.Text.Json;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace JustGo.Api;

public sealed class JustGoExceptionHandler(ILogger<JustGoExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        switch (exception)
        {
            case JustGoApiException justGoException:
                logger.LogError(
                    justGoException,
                    "JustGo API returned status code {UpstreamStatusCode} with body {ResponseBody}.",
                    justGoException.StatusCode,
                    justGoException.Body);

                await Results.Problem(
                    title: "JustGo API request failed",
                    detail: ExtractDetail(justGoException.Body),
                    statusCode: StatusCodes.Status502BadGateway,
                    extensions: new Dictionary<string, object?>
                    {
                        ["upstreamStatusCode"] = justGoException.StatusCode
                    }).ExecuteAsync(httpContext);
                return true;

            case HttpRequestException httpRequestException:
                logger.LogError(httpRequestException, "JustGo API request failed before a response was received.");

                await Results.Problem(
                    title: "JustGo API request failed",
                    detail: httpRequestException.Message,
                    statusCode: StatusCodes.Status502BadGateway).ExecuteAsync(httpContext);
                return true;

            case TaskCanceledException taskCanceledException
                when !ct.IsCancellationRequested && !httpContext.RequestAborted.IsCancellationRequested:
                logger.LogError(taskCanceledException, "JustGo API request timed out.");

                await Results.Problem(
                    title: "JustGo API request timed out",
                    detail: "The request to JustGo timed out before a response was received.",
                    statusCode: StatusCodes.Status504GatewayTimeout).ExecuteAsync(httpContext);
                return true;

            default:
                return false;
        }
    }

    private static string ExtractDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "The JustGo API returned an error response.";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            return TryGetMessage(root, "errors")
                ?? TryGetMessage(root, "detail")
                ?? TryGetMessage(root, "message")
                ?? TryGetMessage(root, "title")
                ?? body;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static string? TryGetMessage(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Object => TryGetMessage(property, "message")
                                    ?? property.EnumerateObject()
                                        .Select(child => TryGetMessage(property, child.Name))
                                        .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message)),
            JsonValueKind.Array => property.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message)),
            _ => null
        };
    }
}
