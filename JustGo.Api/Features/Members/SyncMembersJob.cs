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
    private const int PageSize = 10;

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
                PageSize = PageSize,
                PageNumber = pageNumber,
                Email = "shane.al.rogers@gmail.com",
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

            await UpsertMembersAsync(db, members, syncedAt, context.CancellationToken);

            totalSynced += members.Count;

            logger.LogDebug("Synced page {Page} ({Count} members).", pageNumber, members.Count);

            if (pageNumber >= response.TotalPages || members.Count < PageSize)
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
        foreach (var dto in members)
        {
            var parameters = new object?[]
            {
                dto.Id,
                (object?)dto.FirstName ?? DBNull.Value,
                (object?)dto.LastName ?? DBNull.Value,
                (object?)dto.EmailAddress ?? DBNull.Value,
                (object?)dto.MemberStatus ?? DBNull.Value,
                syncedAt,
                System.Text.Json.JsonSerializer.Serialize(dto)
            };

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO member_sync_records
                    (id, justgo_member_id, first_name, last_name, email_address, member_status, last_synced_at, raw_data)
                VALUES
                    (gen_random_uuid(), {0}, {1}, {2}, {3}, {4}, {5}, {6}::jsonb)
                ON CONFLICT (justgo_member_id) DO UPDATE SET
                    first_name     = EXCLUDED.first_name,
                    last_name      = EXCLUDED.last_name,
                    email_address  = EXCLUDED.email_address,
                    member_status  = EXCLUDED.member_status,
                    last_synced_at = EXCLUDED.last_synced_at,
                    raw_data       = EXCLUDED.raw_data
                """,
                parameters!,
                cancellationToken);
        }
    }
}
