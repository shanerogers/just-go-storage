using JustGo.Extractor.Worker.Extraction.Contracts;
using Quartz;

namespace JustGo.Extractor.Worker.Extraction.Jobs;

[DisallowConcurrentExecution]
public sealed class RunDailyExtractionJob(
    IJustGoExtractionOrchestrator orchestrator,
    ILogger<RunDailyExtractionJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var summary = await orchestrator.RunAsync(cancellationToken);

        if (summary.FailedCount > 0)
        {
            logger.LogError(
                "Extraction batch {BatchId} completed with failures. Failed={Failed}, Errors={Errors}",
                summary.BatchId,
                summary.FailedCount,
                string.Join(" | ", summary.Errors));
        }
    }
}
