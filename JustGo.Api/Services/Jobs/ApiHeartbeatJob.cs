using Quartz;

namespace JustGo.Api.Services.Jobs;

public sealed class ApiHeartbeatJob(ILogger<ApiHeartbeatJob> logger) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Quartz heartbeat job executed at {UtcNow}.", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
