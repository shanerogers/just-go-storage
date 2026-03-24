using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace JustGo.Api.Health;

public sealed class QuartzHealthCheck(ISchedulerFactory schedulerFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            return scheduler.IsStarted && !scheduler.IsShutdown
                ? HealthCheckResult.Healthy("Quartz scheduler is running.")
                : HealthCheckResult.Unhealthy("Quartz scheduler is not running.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Quartz scheduler is unreachable.", ex);
        }
    }
}
