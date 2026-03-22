using JustGo.Extractor.Worker.Extraction.Configuration;
using JustGo.Extractor.Worker.Extraction.Contracts;
using JustGo.Extractor.Worker.Extraction.Documents;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Extractor.Worker.Extraction.Services;

public sealed class CredentialsDomainExtractor(
    IJustGoClient client,
    IOptions<JustGoExtractionOptions> options,
    IDocumentStore documentStore,
    ILogger<CredentialsDomainExtractor> logger) : IJustGoDomainExtractor
{
    public string DomainName => "credentials";

    public async Task<DomainExtractionSummary> ExtractAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var summary = new DomainExtractionSummary { Domain = DomainName };
        var pageSize = options.Value.PageSize;

        await ExtractMemberCredentialsAsync(batchId, pageSize, summary, cancellationToken);
        await ExtractCredentialDefinitionsAsync(batchId, pageSize, summary, cancellationToken);

        return summary;
    }

    private async Task ExtractMemberCredentialsAsync(
        Guid batchId,
        int pageSize,
        DomainExtractionSummary summary,
        CancellationToken cancellationToken)
    {
        var page = 1;
        while (true)
        {
            var request = new FindCredentialsRequest(
                PageNumber: page,
                PageSize: pageSize);

            var response = await client.FindCredentialsByAttributesAsync(request, cancellationToken);
            if (response.Data.Count == 0)
            {
                break;
            }

            foreach (var item in response.Data)
            {
                if (item.CredentialId == Guid.Empty)
                {
                    summary.FailedCount++;
                    logger.LogWarning("Skipping credential with empty credentialId in batch {BatchId}", batchId);
                    continue;
                }

                try
                {
                    var payload = ExtractionSerialization.ToJsonDocument(item);
                    var result = await documentStore.UpsertAsync(
                        new DocumentUpsertRequest(
                            JustGoDocumentType.Credential,
                            item.CredentialId.ToString(),
                            payload,
                            item.ExpiryDate),
                        batchId,
                        cancellationToken);

                    ApplyResult(summary, result);
                }
                catch (Exception ex)
                {
                    summary.FailedCount++;
                    logger.LogError(ex, "Failed to extract credential {CredentialId}", item.CredentialId);
                }
            }

            if (response.PageNumber >= response.TotalPages)
            {
                break;
            }

            page++;
        }
    }

    private async Task ExtractCredentialDefinitionsAsync(
        Guid batchId,
        int pageSize,
        DomainExtractionSummary summary,
        CancellationToken cancellationToken)
    {
        var page = 1;
        while (true)
        {
            var response = await client.GetCredentialDefinitionsAsync(page, pageSize, cancellationToken);
            if (response.Data.Count == 0)
            {
                break;
            }

            foreach (var item in response.Data)
            {
                if (item.CredentialId == Guid.Empty)
                {
                    summary.FailedCount++;
                    logger.LogWarning("Skipping credential definition with empty credentialId in batch {BatchId}", batchId);
                    continue;
                }

                try
                {
                    var payload = ExtractionSerialization.ToJsonDocument(item);
                    var result = await documentStore.UpsertAsync(
                        new DocumentUpsertRequest(
                            JustGoDocumentType.CredentialDefinition,
                            item.CredentialId.ToString(),
                            payload,
                            ModifiedAtSourceUtc: null),
                        batchId,
                        cancellationToken);

                    ApplyResult(summary, result);
                }
                catch (Exception ex)
                {
                    summary.FailedCount++;
                    logger.LogError(ex, "Failed to extract credential definition {CredentialId}", item.CredentialId);
                }
            }

            if (response.PageNumber >= response.TotalPages)
            {
                break;
            }

            page++;
        }
    }

    private static void ApplyResult(DomainExtractionSummary summary, DocumentUpsertResult result)
    {
        switch (result)
        {
            case DocumentUpsertResult.Inserted:
                summary.InsertedCount++;
                break;
            case DocumentUpsertResult.Updated:
                summary.UpdatedCount++;
                break;
            default:
                summary.UnchangedCount++;
                break;
        }
    }
}
