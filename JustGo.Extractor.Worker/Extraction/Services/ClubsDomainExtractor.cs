using JustGo.Extractor.Worker.Extraction.Configuration;
using JustGo.Extractor.Worker.Extraction.Contracts;
using JustGo.Extractor.Worker.Extraction.Documents;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Extractor.Worker.Extraction.Services;

public sealed class ClubsDomainExtractor(
    IJustGoClient client,
    IOptions<JustGoExtractionOptions> options,
    IExtractionJobStateStore stateStore,
    IDocumentStore documentStore,
    ILogger<ClubsDomainExtractor> logger) : IJustGoDomainExtractor
{
    public string DomainName => "clubs";

    public async Task<DomainExtractionSummary> ExtractAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var summary = new DomainExtractionSummary { Domain = DomainName };
        var pageSize = options.Value.PageSize;
        var lastSuccess = await stateStore.GetLastSuccessfulRunUtcAsync(cancellationToken);

        var page = 1;
        while (true)
        {
            var request = new FindClubsRequest(
                ModifiedDate: lastSuccess,
                PageNumber: page,
                PageSize: pageSize);

            var response = await client.FindClubsByAttributesAsync(request, cancellationToken);
            if (response.Data.Count == 0)
            {
                break;
            }

            foreach (var item in response.Data)
            {
                if (item.ClubId == Guid.Empty)
                {
                    summary.FailedCount++;
                    logger.LogWarning("Skipping club with empty clubId in batch {BatchId}", batchId);
                    continue;
                }

                try
                {
                    var payload = ExtractionSerialization.ToJsonDocument(item);
                    var result = await documentStore.UpsertAsync(
                        new DocumentUpsertRequest(
                            JustGoDocumentType.Club,
                            item.ClubId.ToString(),
                            payload,
                            item.ModifiedDate),
                        batchId,
                        cancellationToken);

                    ApplyResult(summary, result);
                }
                catch (Exception ex)
                {
                    summary.FailedCount++;
                    logger.LogError(ex, "Failed to extract club {ClubId}", item.ClubId);
                }
            }

            if (response.PageNumber >= response.TotalPages)
            {
                break;
            }

            page++;
        }

        return summary;
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
