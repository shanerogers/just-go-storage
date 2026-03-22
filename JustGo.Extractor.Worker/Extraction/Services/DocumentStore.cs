using System.Security.Cryptography;
using System.Text;
using JustGo.Extractor.Worker.Extraction.Contracts;
using JustGo.Extractor.Worker.Extraction.Data;
using JustGo.Extractor.Worker.Extraction.Documents;
using Microsoft.EntityFrameworkCore;

namespace JustGo.Extractor.Worker.Extraction.Services;

public sealed class DocumentStore(ExtractionDbContext dbContext) : IDocumentStore
{
    public async Task<DocumentUpsertResult> UpsertAsync(DocumentUpsertRequest request, Guid batchId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var payloadHash = HashPayload(request.Payload.RootElement.GetRawText());

        var entity = await dbContext.JustGoDocuments
            .SingleOrDefaultAsync(
                x => x.DocumentType == request.DocumentType && x.SourceId == request.SourceId,
                cancellationToken);

        if (entity is null)
        {
            entity = new JustGoDocumentEntity
            {
                Id = Guid.NewGuid(),
                DocumentType = request.DocumentType,
                SourceId = request.SourceId,
                Payload = request.Payload,
                PayloadHash = payloadHash,
                Version = 1,
                BatchId = batchId,
                ExtractedAtUtc = now,
                ModifiedAtSourceUtc = request.ModifiedAtSourceUtc
            };
            dbContext.JustGoDocuments.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return DocumentUpsertResult.Inserted;
        }

        if (string.Equals(entity.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            entity.BatchId = batchId;
            entity.ExtractedAtUtc = now;
            entity.ModifiedAtSourceUtc = request.ModifiedAtSourceUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return DocumentUpsertResult.Unchanged;
        }

        entity.Payload = request.Payload;
        entity.PayloadHash = payloadHash;
        entity.Version += 1;
        entity.BatchId = batchId;
        entity.ExtractedAtUtc = now;
        entity.ModifiedAtSourceUtc = request.ModifiedAtSourceUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
        return DocumentUpsertResult.Updated;
    }

    private static string HashPayload(string payloadJson)
    {
        var bytes = Encoding.UTF8.GetBytes(payloadJson);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
