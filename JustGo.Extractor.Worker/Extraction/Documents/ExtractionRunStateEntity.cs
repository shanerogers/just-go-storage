namespace JustGo.Extractor.Worker.Extraction.Documents;

public sealed class ExtractionRunStateEntity
{
    public int Id { get; set; }
    public Guid LastSuccessfulBatchId { get; set; }
    public DateTimeOffset LastSuccessfulCompletedAtUtc { get; set; }
}
