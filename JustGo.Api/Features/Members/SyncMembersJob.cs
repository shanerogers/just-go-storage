using JustGo.Api.Data;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace JustGo.Api.Services.Jobs;

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
        logger.LogInformation("SyncMembersJob starting at {UtcNow}.", timeProvider.GetUtcNow());

        var pageNumber = 1;
        var totalSynced = 0;
        var syncedAt = timeProvider.GetUtcNow();

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var request = new FindMembersRequest
            {
                PageNumber = pageNumber,
                Email = "shane.al.rogers@gmail.com"
                //ModifiedAfter = syncedAt.Subtract(10.Minutes()),
            };

            MembersPagedResponse response;

            try
            {
                response = await justGoClient.FindMembersByAttributesAsync(request, context.CancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch members page {Page} from JustGo API.", pageNumber);
                throw new JobExecutionException(ex, refireImmediately: false);
            }

            var members = response.Data;
            if (members is null || members.Count == 0)
            {
                break;
            }

            await UpsertMembersAsync(db, syncedAt, members, context.CancellationToken);

            totalSynced += members.Count;

            logger.LogDebug("Synced page {Page} ({Count} members).", pageNumber, members.Count);

            if (pageNumber >= response.TotalPages || members.Count < request.PageSize)
            {
                break;
            }

            pageNumber++;
        }

        logger.LogInformation("SyncMembersJob completed at {UtcNow}. Total members synced: {Total}.",
            timeProvider.GetUtcNow(),
            totalSynced);
    }

    private static async Task UpsertMembersAsync(
        ApiDbContext db,
        DateTimeOffset syncedAt,
        List<JustGoMemberDto> members,
        CancellationToken cancellationToken)
    {
        var existingRecords = await db.Members
            .Where(r => members.Select(m => m.Id).Contains(r.JustGoMemberId))
            .ToDictionaryAsync(r => r.JustGoMemberId, cancellationToken);

        foreach (var member in members)
        {
            if (existingRecords.TryGetValue(member.Id, out var record))
            {
                record.FirstName = member.FirstName;
                record.LastName = member.LastName;
                record.EmailAddress = member.EmailAddress;
                record.MemberStatus = member.MemberStatus;
                record.LastSyncedAt = syncedAt;
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
                    LastSyncedAt = syncedAt,
                    RawData = member
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
