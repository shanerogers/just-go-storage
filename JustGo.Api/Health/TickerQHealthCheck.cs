using Microsoft.Extensions.Diagnostics.HealthChecks;
using TickerQ.Utilities.Interfaces;

namespace JustGo.Api.Health;

public sealed class TickerQHealthCheck(ITickerQHostScheduler scheduler) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => Task.FromResult(scheduler.IsRunning
            ? HealthCheckResult.Healthy("TickerQ scheduler is running.")
            : HealthCheckResult.Unhealthy("TickerQ scheduler is not running."));
}
