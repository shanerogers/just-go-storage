namespace JustGo.Extractor.Worker.Extraction.Contracts;

public interface IJustGoExtractionOrchestrator
{
    Task<ExtractionBatchSummary> RunAsync(CancellationToken cancellationToken);
}
