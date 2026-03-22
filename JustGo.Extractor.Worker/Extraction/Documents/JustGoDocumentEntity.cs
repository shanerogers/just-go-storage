using System.Text.Json;

namespace JustGo.Extractor.Worker.Extraction.Documents;

public sealed class JustGoDocumentEntity
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public string PayloadHash { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Guid BatchId { get; set; }
    public DateTimeOffset ExtractedAtUtc { get; set; }
    public DateTimeOffset? ModifiedAtSourceUtc { get; set; }
}
