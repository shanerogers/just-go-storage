using System.Runtime.CompilerServices;
using ErrorOr;
using JustGo.Api.Data;
using Microsoft.EntityFrameworkCore;
using Humanizer;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Base;

namespace JustGo.Api.Features.Members;

/// <summary>
/// Background sync task that pages through the JustGo Members API and inserts or updates each member
/// into the local <c>member_sync_records</c> Postgres table.
/// </summary>
public sealed class SyncMembersJob(
    TimeProvider timeProvider,
    IMemberClient memberClient,
    ILogger<SyncMembersJob> logger,
    IServiceScopeFactory scopeFactory) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var pageNo = 1;
        var totalSynced = 0;
        var failedPages = 0;
        var syncedAtUtc = timeProvider.GetUtcNow();

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        await foreach (var result in ProcessPagesAsync(syncedAtUtc, db, cancellationToken))
        {
            result
                .ThenDo(outcome => totalSynced += outcome.SyncedCount)
                .ElseDo(errors =>
                {
                    failedPages++;
                    logger.LogError("SyncMembersJob failed on page {Page}: {Code} - {Description}.",
                        pageNo,
                        errors[0].Code,
                        errors[0].Description);
                });

            if (result.IsError) throw CreateSyncException(result.FirstError, pageNo);
            pageNo++;
        }

        logger.LogInformation(
            "SyncMembersJob completed at {UtcNow}. Total members synced: {Total}. Failed pages: {FailedPages}.",
            timeProvider.GetUtcNow(),
            totalSynced,
            failedPages);
    }

    private async IAsyncEnumerable<ErrorOr<PageOutcome>> ProcessPagesAsync(
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var pageNumber = 1;
        var shouldContinue = true;
        while (shouldContinue && !ct.IsCancellationRequested)
        {
            yield return await ProcessSinglePageAsync(pageNumber, syncedAtUtc, db, ct)
                .ElseDo(_ => shouldContinue = false)
                .ThenDo(outcome =>
                {
                    pageNumber = outcome.NextPageNumber;
                    shouldContinue = outcome.ShouldContinue;
                });
        }
    }

    private async Task<ErrorOr<PageOutcome>> ProcessSinglePageAsync(
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        CancellationToken ct)
    {
        var pageResult = await FetchPageAsync(pageNumber, syncedAtUtc, ct);
        return await pageResult
            .ThenDo(page => logger.LogDebug("Fetched members page {Page} with {Count} records (total pages: {TotalPages}).",
                pageNumber,
                page.Members.Count,
                page.Response.TotalPages))
            .ThenAsync(page => ProcessPageMembersAsync(page, pageNumber, syncedAtUtc, db, ct))
            .ElseDo(errors => logger.LogWarning("Failed processing page {Page}: {Code} - {Description}.",
                pageNumber,
                errors[0].Code,
                errors[0].Description));
    }

    private async Task<ErrorOr<PageOutcome>> ProcessPageMembersAsync(
        MemberPage page,
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        ApiDbContext db,
        CancellationToken ct)
    {
        var attemptedCount = 0;
        var failedCount = 0;
        var syncedCount = 0;
        foreach (var memberId in page.Members.Select(m => m.Id))
        {
            attemptedCount++;
            await FetchMemberDetailAsync(page, memberId, ct)
                .ThenAsync(member => UpsertMemberAsync(db, syncedAtUtc, member, ct))
                .ThenDo(_ => syncedCount++)
                .ElseDo(errors =>
                {
                    failedCount++;
                    logger.LogWarning("Skipping member {MemberId} on page {Page}: {Code} - {Description}.",
                        memberId,
                        pageNumber,
                        errors[0].Code,
                        errors[0].Description);
                });
        }

        LogPageCompleted(pageNumber, syncedCount, attemptedCount, failedCount);

        // Continue if we received a full page — the simplest reliable signal that there are more.
        // Also respect TotalRecords when available as a double-check.
        var receivedFullPage = page.Members.Count >= page.Response.PageSize && page.Response.PageSize > 0;
        var moreByTotals = page.Response.TotalRecords > 0
            && page.Response.PageSize > 0
            && pageNumber * page.Response.PageSize < page.Response.TotalRecords;

        var shouldContinue = page.Members.Count > 0 && (receivedFullPage || moreByTotals);

        logger.LogInformation(
            "Page {Page}: {Members}/{PageSize} members, TotalRecords={TotalRecords}, TotalPages={TotalPages}. ShouldContinue={ShouldContinue}.",
            pageNumber,
            page.Members.Count,
            page.Response.PageSize,
            page.Response.TotalRecords,
            page.Response.TotalPages,
            shouldContinue);

        return new PageOutcome(syncedCount, shouldContinue, pageNumber + 1);
    }

    private async Task<ErrorOr<MemberDetail>> FetchMemberDetailAsync(
        MemberPage page,
        Guid memberId,
        CancellationToken ct)
    {
        try
        {
            var detail = await memberClient.GetMemberAsync(memberId, ct);
            return new MemberDetail(detail, page.Response, page.Members.Count);
        }
        catch (Exception ex)
        {
            return CreateFailure(
                code: "members.fetch_detail_failed",
                description: $"Failed to fetch detail for member {memberId}: {ex.Message}",
                exception: ex);
        }
    }

    private static Exception CreateSyncException(Error error, int pageNumber)
    {
        return new Exception($"Failed to process page {pageNumber}: {error.Description}");
    }

    private async Task<ErrorOr<MemberPage>> FetchPageAsync(
        int pageNumber,
        DateTimeOffset syncedAtUtc,
        CancellationToken ct)
    {
        try
        {
            var request = new FindMembersRequest
            {
                PageSize = 100,
                PageNumber = pageNumber,
                ModifiedBefore = syncedAtUtc,
                ModifiedAfter = In.AprilOf(2005)
            };
            var response = await memberClient.FindMembersByAttributesAsync(request, ct);
            return new MemberPage(response, response.Data ?? []);
        }
        catch (Exception ex)
        {
            return CreateFailure(
                code: "members.fetch_page_failed",
                description: $"Failed to fetch members page {pageNumber}: {ex.Message}",
                exception: ex);
        }
    }

    private static async Task<ErrorOr<MemberDetail>> UpsertMemberAsync(
        ApiDbContext database,
        DateTimeOffset syncedAtUtc,
        MemberDetail member,
        CancellationToken ct)
    {
        try
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

            return member;
        }
        catch (Exception ex)
        {
            return CreateFailure(
                code: "members.upsert_failed",
                description: $"Failed to upsert member {member.Detail.Id}: {ex.Message}",
                exception: ex);
        }
    }

    private static Error CreateFailure(string code, string description, Exception exception) =>
        Error.Failure(code, $"{description} (Exception: {exception.GetType().Name})");

    private void LogPageCompleted(int pageNumber, int syncedCount, int attemptedCount, int failedCount) =>
        logger.LogDebug("Synced page {Page}: {SyncedCount} of {AttemptedCount} members (failed: {FailedCount}).",
        pageNumber,
        syncedCount,
        attemptedCount,
        failedCount);
}

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
    bool ShouldContinue,
    int NextPageNumber);
