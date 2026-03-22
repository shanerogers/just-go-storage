namespace JustGo.Extractor.Worker.Extraction.Contracts;

public sealed class DomainExtractionSummary
{
    public required string Domain { get; init; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int UnchangedCount { get; set; }
    public int FailedCount { get; set; }
}
