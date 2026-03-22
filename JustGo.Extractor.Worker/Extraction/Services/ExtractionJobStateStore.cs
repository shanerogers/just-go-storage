using JustGo.Extractor.Worker.Extraction.Contracts;
using JustGo.Extractor.Worker.Extraction.Data;
using JustGo.Extractor.Worker.Extraction.Documents;
using Microsoft.EntityFrameworkCore;

namespace JustGo.Extractor.Worker.Extraction.Services;

public sealed class ExtractionJobStateStore(ExtractionDbContext dbContext) : IExtractionJobStateStore
{
    private const int SingletonId = 1;

    public async Task<DateTimeOffset?> GetLastSuccessfulRunUtcAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ExtractionRunState
            .Where(x => x.Id == SingletonId)
            .Select(x => (DateTimeOffset?)x.LastSuccessfulCompletedAtUtc)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RecordSuccessAsync(Guid batchId, DateTimeOffset completedAtUtc, CancellationToken cancellationToken)
    {
        var state = await dbContext.ExtractionRunState
            .SingleOrDefaultAsync(x => x.Id == SingletonId, cancellationToken);

        if (state is null)
        {
            state = new ExtractionRunStateEntity
            {
                Id = SingletonId,
                LastSuccessfulBatchId = batchId,
                LastSuccessfulCompletedAtUtc = completedAtUtc
            };
            dbContext.ExtractionRunState.Add(state);
        }
        else
        {
            state.LastSuccessfulBatchId = batchId;
            state.LastSuccessfulCompletedAtUtc = completedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
