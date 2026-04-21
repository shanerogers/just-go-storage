using System.Runtime.CompilerServices;
using LanguageExt;
using static LanguageExt.Prelude;
using JustGo.Api.Data;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Humanizer;

namespace JustGo.Api.Services.Jobs;

internal sealed record SyncError(
    string Code,
    string Description,
    Exception? Exception = null);

/// <summary>
/// A page of members returned from the JustGo search endpoint.
/// </summary>
internal record MemberPage(
    MembersPagedResponse Response,
    List<JustGoMemberDto> Members);

/// <summary>
/// A page enriched with full member details.
/// </summary>
internal record MemberDetail(
    MemberDetailDto Detail,
    MembersPagedResponse Response,
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
    TimeProvider timeProvider,
    IJustGoClient justGoClient,
    ILogger<SyncMembersJob> logger,
    IServiceScopeFactory scopeFactory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        int pageNo = 1;
        int totalSynced = 0;
        var syncedAtUtc = timeProvider.GetUtcNow();

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        await foreach (var result in ProcessPagesAsync(syncedAtUtc, db, context.CancellationToken))
        {
            if (result.IsLeft) throw CreateJobExecutionException(result.LeftToList()[0], pageNo);
            result.IfRight(outcome => totalSynced += outcome.SyncedCount);
            pageNo++;
        }

        logger.LogInformation("SyncMembersJob completed at {UtcNow}. Total members synced: {Total}.",
            timeProvider.GetUtcNow(),
            totalSynced);
    }

    private async IAsyncEnumerable<Either<SyncError, PageOutcome>> ProcessPagesAsync(
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int pageNumber = 1;
        bool shouldContinue = true;

        while (shouldContinue && !ct.IsCancellationRequested)
        {
            var result = await ProcessSinglePageAsync(pageNumber, syncedAtUtc, db, ct);

            shouldContinue = result.Match(
                Right: outcome => outcome.ShouldContinue,
                Left: _ => false);

            pageNumber++;
            yield return result;
        }
    }

    private EitherAsync<SyncError, PageOutcome> ProcessSinglePageAsync(
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        CancellationToken ct) => FetchPageAsync(pageNumber, syncedAtUtc, ct)
            .Bind(page => ProcessPageMembersAsync(page, pageNumber, syncedAtUtc, db, ct)
            .ToAsync());

    private async Task<Either<SyncError, PageOutcome>> ProcessPageMembersAsync(
        MemberPage page,
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        CancellationToken ct)
    {
        int syncedCount = 0;
        MemberDetail? lastMember = null;

        await foreach (var memberResult in CollectMemberDetailsAsync(page, ct))
        {
            var stepResult = await memberResult.ToAsync()
                .Bind(member => UpsertMemberAsync(db, syncedAtUtc, member, ct).Map(_ => member))
                .Map(member =>
                {
                    syncedCount++;
                    lastMember = member;
                    return member;
                });

            if (stepResult.IsLeft) return stepResult.LeftToList()[0];
        }

        LogPageCompleted(pageNumber, syncedCount);

        var shouldContinue = lastMember is not null
            && ShouldContinue(pageNumber, lastMember.Response, lastMember.SourceCount);

        return new PageOutcome(syncedCount, shouldContinue);
    }

    private async IAsyncEnumerable<Either<SyncError, MemberDetail>> CollectMemberDetailsAsync(
        MemberPage page,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var memberId in page.Members.Select(m => m.Id))
        {
            yield return await TryAsync(() => justGoClient.GetMemberAsync(memberId, ct))
                .ToEither(ex => new SyncError(
                    Code: "members.fetch_detail_failed",
                    Description: $"Failed to fetch detail for member {memberId}: {ex.Message}",
                    Exception: ex))
                .Map(detail => new MemberDetail(detail, page.Response, page.Members.Count));
        }
    }

    private static JobExecutionException CreateJobExecutionException(SyncError error, int pageNumber)
    {
        return new JobExecutionException(
            new Exception($"Failed to process page {pageNumber}: {error.Description}", error.Exception),
            refireImmediately: false);
    }

    private EitherAsync<SyncError, MemberPage> FetchPageAsync(
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        CancellationToken ct) =>
        TryAsync(async () =>
        {
            var request = new FindMembersRequest
            {
                PageSize = 100,
                PageNumber = pageNumber,
                ModifiedBefore = syncedAtUtc,
                ModifiedAfter = In.AprilOf(2005)
            };
            var response = await justGoClient.FindMembersByAttributesAsync(request, ct);
            return new MemberPage(response, response.Data ?? []);
        })
        .ToEither(ex => new SyncError(
            Code: "members.fetch_page_failed",
            Description: $"Failed to fetch members page {pageNumber}: {ex.Message}",
            Exception: ex));

    private static bool ShouldContinue(
        int currentPage,
        MembersPagedResponse response,
        int pageSize) =>
        currentPage < response.TotalPages && pageSize >= new FindMembersRequest().PageSize;

    private static EitherAsync<SyncError, Unit> UpsertMemberAsync(
        ApiDbContext database,
        DateTimeOffset syncedAtUtc,
        MemberDetail member,
        CancellationToken ct) =>
        TryAsync(async () =>
        {
            var existing = await database.Members
                .TagWith("Get existing member sync record for update")
                .FirstOrDefaultAsync(r => r.JustGoMemberId == member.Detail.Id, ct);

            if (existing is not null)
            {
                existing.MemberInformation = member.Detail;
                existing.LastSyncedAt = syncedAtUtc;
                existing.LastName = member.Detail.LastName;
                existing.FirstName = member.Detail.FirstName;
                existing.EmailAddress = member.Detail.EmailAddress;
                existing.MemberStatus = member.Detail.MemberStatus;
            }
            else
            {
                database.Members.Add(new MemberSyncRecord
                {
                    MemberInformation = member.Detail,
                    LastSyncedAt = syncedAtUtc,
                    JustGoMemberId = member.Detail.Id,
                    LastName = member.Detail.LastName,
                    FirstName = member.Detail.FirstName,
                    EmailAddress = member.Detail.EmailAddress,
                    MemberStatus = member.Detail.MemberStatus,
                });
            }

            await database.SaveChangesAsync(ct);
            return unit;
        })
        .ToEither(ex => new SyncError(
            Code: "members.upsert_failed",
            Description: $"Failed to upsert member {member.Detail.Id}: {ex.Message}",
            Exception: ex));

    private void LogPageCompleted(int pageNumber, int count) => logger.LogDebug("Synced page {Page} ({Count} members).",
        pageNumber,
        count);
}
