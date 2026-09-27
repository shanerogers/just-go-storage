using Microsoft.Extensions.Http.Resilience;

namespace JustGo.Grading;

/// <summary>
/// Timeouts for calls from the grading app to our API. The API forwards to JustGo, whose sandbox
/// routinely takes 4-9s per call, so the 10s default attempt timeout cancelled healthy requests.
/// </summary>
public static class JustGoApiResilience
{
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(45);
    public static readonly TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(100);
    public const int MaxRetryAttempts = 1;

    public static void Configure(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = AttemptTimeout;
        options.TotalRequestTimeout.Timeout = TotalRequestTimeout;
        options.Retry.MaxRetryAttempts = MaxRetryAttempts;
        options.Retry.DisableForUnsafeHttpMethods();

        // The circuit breaker requires a sampling window of at least twice the attempt timeout.
        options.CircuitBreaker.SamplingDuration = AttemptTimeout * 2;
    }
}
