using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using JustGo.Api.Services;

namespace JustGo.Api.Tests.Services;

public sealed class JustGoTelemetryHandlerTests
{
    [Fact]
    public async Task SendAsync_SuccessfulRequest_RecordsSanitizedDurationMetricAndTrace()
    {
        var measurements = new List<MetricMeasurement>();
        using var meterListener = CreateMeterListener(measurements);
        using var activityListener = CreateActivityListener(out var completedActivities);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        using var response = await client.GetAsync(
            "https://api.justgo.com/v2.2/members/11111111-1111-1111-1111-111111111111?apiKey=secret");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var duration = Assert.Single(measurements, measurement => measurement.Name == "justgo.http.client.duration");
        Assert.True(duration.Value >= 0);
        Assert.Equal("GET /v2.2/members/{id}", duration.Tags["justgo.operation"]);
        Assert.Equal("api.justgo.com", duration.Tags["server.address"]);
        Assert.Equal("success", duration.Tags["outcome"]);
        Assert.DoesNotContain("secret", string.Join(' ', duration.Tags.Values));
        Assert.DoesNotContain("11111111-1111-1111-1111-111111111111", string.Join(' ', duration.Tags.Values));

        var activity = Assert.Single(completedActivities);
        var activityTags = ToDictionary(activity.Tags);
        Assert.Equal("GET /v2.2/members/{id}", activity.DisplayName);
        Assert.Equal("justgo", activityTags["peer.service"]);
        Assert.Equal("/v2.2/members/{id}", activityTags["url.template"]);
    }

    [Fact]
    public async Task SendAsync_FailedResponse_RecordsFailureMetric()
    {
        var measurements = new List<MetricMeasurement>();
        using var meterListener = CreateMeterListener(measurements);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using var response = await client.PostAsync("https://api.justgo.com/v2.2/events/42", content: null);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var failure = Assert.Single(measurements, measurement => measurement.Name == "justgo.http.client.failures");
        Assert.Equal("http_error", failure.Tags["outcome"]);
        Assert.Equal("429", failure.Tags["http.response.status_code"]);
        Assert.Equal("POST /v2.2/events/{id}", failure.Tags["justgo.operation"]);
    }

    private static MeterListener CreateMeterListener(List<MetricMeasurement> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == JustGoTelemetryHandler.TelemetryName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            measurements.Add(new MetricMeasurement(instrument.Name, measurement, ToDictionary(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            measurements.Add(new MetricMeasurement(instrument.Name, measurement, ToDictionary(tags))));
        listener.Start();
        return listener;
    }

    private static ActivityListener CreateActivityListener(out List<Activity> completedActivities)
    {
        completedActivities = [];
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == JustGoTelemetryHandler.TelemetryName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = completedActivities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> send) =>
        new(new JustGoTelemetryHandler { InnerHandler = new StubHttpMessageHandler(send) });

    private static Dictionary<string, string> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (var tag in tags)
        {
            dictionary[tag.Key] = Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return dictionary;
    }

    private static Dictionary<string, string> ToDictionary(IEnumerable<KeyValuePair<string, string?>> tags)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (var tag in tags)
        {
            dictionary[tag.Key] = tag.Value ?? string.Empty;
        }

        return dictionary;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(send(request));
    }

    private sealed record MetricMeasurement(string Name, double Value, Dictionary<string, string> Tags)
    {
        public MetricMeasurement(string name, long value, Dictionary<string, string> tags)
            : this(name, (double)value, tags)
        {
        }
    }
}
