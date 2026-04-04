using ErrorOr;
using JustGo.Api.Data;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace JustGo.Api.Services.Jobs;

/// <summary>
/// A page of members returned from the JustGo search endpoint.
/// </summary>
internal record MemberPage(
    MembersPagedResponse Response,
    List<JustGoMemberDto> Members);

/// <summary>
/// A page enriched with full member details.
/// </summary>
internal record MemberDetailsPage(
    MembersPagedResponse Response,
    List<MemberDetailDto> Details,
    int SourceCount);

/// <summary>
/// Outcome of processing a page.
/// </summary>
internal record PageOutcome(
    int SyncedCount,
    bool ShouldContinue);

/// <summary>
/// Quartz job that pages through the JustGo Members API and inserts or updates each member
/// into the local <c>member_sync_records</c> Postgres table.
/// </summary>
[DisallowConcurrentExecution]
public sealed class SyncMembersJob(
    IJustGoClient justGoClient,
    TimeProvider timeProvider,
    ILogger<SyncMembersJob> logger,
    IServiceScopeFactory scopeFactory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        int currentPage = 1;
        int totalSynced = 0;
        var syncedAtUtc = timeProvider.GetUtcNow();

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var outcome = await ProcessPageAsync(currentPage, syncedAtUtc, db, context.CancellationToken)
                .MatchFirst(
                    value => value,
                    error => throw CreateJobExecutionException(error, currentPage));

            totalSynced += outcome.SyncedCount;

            if (!outcome.ShouldContinue)
            {
                break;
            }

            currentPage++;
        }

        logger.LogInformation("SyncMembersJob completed at {UtcNow}. Total members synced: {Total}.",
            timeProvider.GetUtcNow(),
            totalSynced);
    }

    private async Task<ErrorOr<PageOutcome>> ProcessPageAsync(
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        CancellationToken cancellationToken)
    {
        return await FetchPageAsync(pageNumber, syncedAtUtc, cancellationToken)
            .ThenAsync(page => FetchMemberDetailsAsync(page, cancellationToken))
            .ThenAsync(page => UpsertMembersAsync(db, syncedAtUtc, pageNumber, page, cancellationToken))
            .ThenDo(outcome => LogPageCompleted(pageNumber, outcome.SyncedCount))
            .Else(errors => Error.Failure(
                "members.process_page_failed",
                $"Failed to process members page {pageNumber}: {errors[0].Description}"));
    }

    private static JobExecutionException CreateJobExecutionException(Error error, int pageNumber)
    {
        var innerException = error.Metadata is not null
            && error.Metadata.TryGetValue("exception", out var exception)
            ? exception as Exception
            : null;

        return new JobExecutionException(
            new Exception($"Failed to process page {pageNumber}: {error.Description}", innerException),
            refireImmediately: false);
    }

    private async Task<ErrorOr<MemberPage>> FetchPageAsync(
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new FindMembersRequest
            {
                PageNumber = pageNumber,
                ModifiedBefore = syncedAtUtc,
                Email = "shane.al.rogers@gmail.com",
            };

            var response = await justGoClient.FindMembersByAttributesAsync(request, cancellationToken);
            return new MemberPage(response, response.Data ?? []);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                code: "members.fetch_page_failed",
                description: $"Failed to fetch members page {pageNumber}: {ex.Message}",
                metadata: new Dictionary<string, object>
                {
                    ["exception"] = ex,
                });
        }
    }

    private async Task<ErrorOr<MemberDetailsPage>> FetchMemberDetailsAsync(
        MemberPage page,
        CancellationToken cancellationToken)
    {
        if (page.Members.Count == 0)
        {
            return new MemberDetailsPage(page.Response, [], 0);
        }

        var details = new List<MemberDetailDto>(page.Members.Count);

        foreach (var memberId in page.Members.Select(member => member.Id))
        {
            try
            {
                var detail = await justGoClient.GetMemberAsync(memberId, cancellationToken);
                details.Add(detail);
            }
            catch (Exception ex)
            {
                LogMemberDetailSkipped(memberId, ex);
            }
        }

        return new MemberDetailsPage(page.Response, details, page.Members.Count);
    }

    private static bool ShouldContinue(
        int currentPage,
        MembersPagedResponse response,
        int pageSize) =>
        currentPage < response.TotalPages && pageSize >= new FindMembersRequest().PageSize;

    private async Task<ErrorOr<PageOutcome>> UpsertMembersAsync(
        ApiDbContext db,
        DateTimeOffset syncedAtUtc,
        int pageNumber,
        MemberDetailsPage page,
        CancellationToken cancellationToken)
    {
        if (page.Details.Count == 0)
        {
            return new PageOutcome(
                SyncedCount: 0,
                ShouldContinue: ShouldContinue(pageNumber, page.Response, page.SourceCount));
        }

        try
        {
            var memberIds = page.Details.Select(member => member.Id).ToHashSet();
            var existingRecords = await db.Members
                .Where(record => memberIds.Contains(record.JustGoMemberId))
                .ToDictionaryAsync(r => r.JustGoMemberId, cancellationToken);

            foreach (var member in page.Details)
            {
                if (existingRecords.TryGetValue(member.Id, out var record))
                {
                    record.FirstName = member.FirstName;
                    record.LastName = member.LastName;
                    record.EmailAddress = member.EmailAddress;
                    record.MemberStatus = member.MemberStatus;
                    record.LastSyncedAt = syncedAtUtc;
                    record.RawData = member;
                }
                else
                {
                    db.Members.Add(new MemberSyncRecord
                    {
                        JustGoMemberId = member.Id,
                        FirstName = member.FirstName,
                        LastName = member.LastName,
                        EmailAddress = member.EmailAddress,
                        MemberStatus = member.MemberStatus,
                        LastSyncedAt = syncedAtUtc,
                        RawData = member
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return new PageOutcome(
                SyncedCount: page.Details.Count,
                ShouldContinue: ShouldContinue(pageNumber, page.Response, page.SourceCount));
        }
        catch (Exception ex)
        {
            return Error.Failure(
                code: "members.upsert_failed",
                description: $"Failed to upsert members: {ex.Message}",
                metadata: new Dictionary<string, object>
                {
                    ["exception"] = ex,
                });
        }
    }

    private void LogPageCompleted(int pageNumber, int count) =>
        logger.LogDebug("Synced page {Page} ({Count} members).", pageNumber, count);

    private void LogMemberDetailSkipped(Guid memberId, Exception ex) =>
        logger.LogWarning(ex, "Failed to fetch member detail for {MemberId}; skipping.", memberId);
}
