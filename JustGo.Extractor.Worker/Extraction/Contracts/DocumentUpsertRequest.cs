using System.Text.Json;

namespace JustGo.Extractor.Worker.Extraction.Contracts;

public sealed record DocumentUpsertRequest(
    string DocumentType,
    string SourceId,
    JsonDocument Payload,
    DateTimeOffset? ModifiedAtSourceUtc);
