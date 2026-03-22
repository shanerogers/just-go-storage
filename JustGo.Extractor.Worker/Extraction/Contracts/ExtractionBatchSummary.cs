namespace JustGo.Extractor.Worker.Extraction.Contracts;

public sealed class ExtractionBatchSummary
{
    public required Guid BatchId { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int UnchangedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; } = [];
    public List<DomainExtractionSummary> Domains { get; } = [];

    public int ProcessedCount => InsertedCount + UpdatedCount + UnchangedCount + FailedCount;
}
