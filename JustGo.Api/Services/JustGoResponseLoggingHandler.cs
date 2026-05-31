using System.Diagnostics;

namespace JustGo.Api.Services;

/// <summary>
/// Logs full JustGo HTTP request/response details including response body.
/// Only reads the body at Debug level to avoid unnecessary allocations in production.
/// </summary>
internal sealed class JustGoResponseLoggingHandler(ILogger<JustGoResponseLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;

        try
        {
            response = await base.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "JustGo {Method} {Uri} failed after {ElapsedMs}ms",
                request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds);
            throw;
        }

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "JustGo {Method} {Uri} → {StatusCode} in {ElapsedMs}ms — {Body}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                Truncate(errorBody));
        }
        else if (logger.IsEnabled(LogLevel.Debug))
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogDebug(
                "JustGo {Method} {Uri} → {StatusCode} in {ElapsedMs}ms — {Body}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                Truncate(body));
        }
        else
        {
            logger.LogInformation(
                "JustGo {Method} {Uri} → {StatusCode} in {ElapsedMs}ms",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    private static string Truncate(string value, int maxLength = 2000) =>
        value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "…[truncated]");
}
