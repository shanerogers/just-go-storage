namespace JustGo.Extractor.Worker.Extraction.Contracts;

public interface IJustGoDomainExtractor
{
    string DomainName { get; }
    Task<DomainExtractionSummary> ExtractAsync(Guid batchId, CancellationToken cancellationToken);
}
