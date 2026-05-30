using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace JustGo.Api.Features.Members;

public static class SyncAdminEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapSyncAdminEndpoints()
        {
            var group = app.MapGroup("/admin/sync").WithTags("Sync");

            group.MapPost("/members", async (ITimeTickerManager<TimeTickerEntity> timeTicker, CancellationToken ct) =>
            {
                var scheduleResult = await timeTicker.AddAsync<SyncMembersJob>(DateTime.UtcNow, ct);

                if (!scheduleResult.IsSucceeded)
                {
                    return Results.Problem(title: "Failed to trigger member sync");
                }

                return Results.Accepted();
            })
            .WithName("TriggerMemberSync")
            .WithSummary("Trigger an on-demand member sync");

            return app;
        }
    }
}
