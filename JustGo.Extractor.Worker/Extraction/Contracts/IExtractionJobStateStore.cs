namespace JustGo.Extractor.Worker.Extraction.Contracts;

public interface IExtractionJobStateStore
{
    Task<DateTimeOffset?> GetLastSuccessfulRunUtcAsync(CancellationToken cancellationToken);
    Task RecordSuccessAsync(Guid batchId, DateTimeOffset completedAtUtc, CancellationToken cancellationToken);
}
