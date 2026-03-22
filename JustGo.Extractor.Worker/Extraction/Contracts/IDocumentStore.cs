namespace JustGo.Extractor.Worker.Extraction.Contracts;

public interface IDocumentStore
{
    Task<DocumentUpsertResult> UpsertAsync(DocumentUpsertRequest request, Guid batchId, CancellationToken cancellationToken);
}
