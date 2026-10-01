using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

namespace JustGo.Api.Services;

internal sealed class JustGoTelemetryHandler : DelegatingHandler
{
    internal const string TelemetryName = "JustGo.Api.Integrations.JustGo";

    private static readonly ActivitySource ActivitySource = new(TelemetryName);
    private static readonly Meter Meter = new(TelemetryName);
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "justgo.http.client.requests",
        unit: "{request}",
        description: "Number of HTTP requests sent to JustGo.");
    private static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>(
        "justgo.http.client.failures",
        unit: "{failure}",
        description: "Number of failed HTTP requests sent to JustGo.");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "justgo.http.client.duration",
        unit: "ms",
        description: "Duration of HTTP requests sent to JustGo.");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var operation = CreateOperationName(request);
        var startedAt = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity(operation, ActivityKind.Client);
        activity?.SetTag("http.request.method", request.Method.Method);
        activity?.SetTag("server.address", request.RequestUri?.Host);
        activity?.SetTag("url.template", GetSafePath(request.RequestUri));
        activity?.SetTag("peer.service", "justgo");

        try
        {
            var response = await base.SendAsync(request, ct);
            var outcome = response.IsSuccessStatusCode ? "success" : "http_error";
            var tags = CreateTags(request, operation, outcome, response.StatusCode);

            RecordRequest(Stopwatch.GetElapsedTime(startedAt), tags, !response.IsSuccessStatusCode);
            activity?.SetTag("http.response.status_code", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {(int)response.StatusCode}");
            }

            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            RecordFailure(request, operation, "canceled", null, Stopwatch.GetElapsedTime(startedAt));
            activity?.SetStatus(ActivityStatusCode.Error, "Request canceled");
            throw;
        }
        catch (OperationCanceledException)
        {
            RecordFailure(request, operation, "timeout", null, Stopwatch.GetElapsedTime(startedAt));
            activity?.SetStatus(ActivityStatusCode.Error, "Request timed out");
            throw;
        }
        catch (HttpRequestException ex)
        {
            RecordFailure(request, operation, "network_error", null, Stopwatch.GetElapsedTime(startedAt));
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
    }

    private static void RecordFailure(
        HttpRequestMessage request,
        string operation,
        string outcome,
        HttpStatusCode? statusCode,
        TimeSpan elapsed)
    {
        var tags = CreateTags(request, operation, outcome, statusCode);
        RecordRequest(elapsed, tags, failed: true);
    }

    private static void RecordRequest(TimeSpan elapsed, TagList tags, bool failed)
    {
        RequestCounter.Add(1, tags);
        Duration.Record(elapsed.TotalMilliseconds, tags);

        if (failed)
        {
            FailureCounter.Add(1, tags);
        }
    }

    private static TagList CreateTags(
        HttpRequestMessage request,
        string operation,
        string outcome,
        HttpStatusCode? statusCode)
    {
        var tags = new TagList
        {
            { "justgo.operation", operation },
            { "http.request.method", request.Method.Method },
            { "server.address", request.RequestUri?.Host ?? "unknown" },
            { "outcome", outcome },
        };

        if (statusCode is not null)
        {
            tags.Add("http.response.status_code", (int)statusCode);
        }

        return tags;
    }

    private static string CreateOperationName(HttpRequestMessage request) =>
        $"{request.Method.Method} {GetSafePath(request.RequestUri)}";

    private static string GetSafePath(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return "/";
        }

        var segments = requestUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var safeSegments = segments.Select(segment =>
            Guid.TryParse(segment, out _) || long.TryParse(segment, out _)
                ? "{id}"
                : segment);

        return $"/{string.Join('/', safeSegments)}";
    }
}
