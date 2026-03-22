using JustGo.Extractor.Worker.Extraction.Contracts;

namespace JustGo.Extractor.Worker.Extraction.Services;

public sealed class JustGoExtractionOrchestrator(
    IEnumerable<IJustGoDomainExtractor> extractors,
    IExtractionJobStateStore stateStore,
    ILogger<JustGoExtractionOrchestrator> logger) : IJustGoExtractionOrchestrator
{
    public async Task<ExtractionBatchSummary> RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var batchId = Guid.NewGuid();
        var summary = new ExtractionBatchSummary
        {
            BatchId = batchId,
            StartedAtUtc = startedAt
        };

        logger.LogInformation("Starting JustGo extraction batch {BatchId}", batchId);

        foreach (var extractor in extractors)
        {
            try
            {
                var domainSummary = await extractor.ExtractAsync(batchId, cancellationToken);
                summary.Domains.Add(domainSummary);
                summary.InsertedCount += domainSummary.InsertedCount;
                summary.UpdatedCount += domainSummary.UpdatedCount;
                summary.UnchangedCount += domainSummary.UnchangedCount;
                summary.FailedCount += domainSummary.FailedCount;
            }
            catch (Exception ex)
            {
                summary.FailedCount++;
                summary.Errors.Add($"Domain {extractor.DomainName} failed: {ex.Message}");
                logger.LogError(ex, "Domain extractor {DomainName} failed in batch {BatchId}", extractor.DomainName, batchId);
            }
        }

        summary.CompletedAtUtc = DateTimeOffset.UtcNow;

        if (summary.FailedCount == 0)
        {
            await stateStore.RecordSuccessAsync(batchId, summary.CompletedAtUtc, cancellationToken);
        }

        logger.LogInformation(
            "Completed JustGo extraction batch {BatchId}. Processed={Processed}, Inserted={Inserted}, Updated={Updated}, Unchanged={Unchanged}, Failed={Failed}",
            summary.BatchId,
            summary.ProcessedCount,
            summary.InsertedCount,
            summary.UpdatedCount,
            summary.UnchangedCount,
            summary.FailedCount);

        return summary;
    }
}
