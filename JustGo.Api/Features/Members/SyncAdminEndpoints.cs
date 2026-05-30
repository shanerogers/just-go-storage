using TickerQ.Utilities.Base;

namespace JustGo.Api.Features.Members;

public static class SyncAdminEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapSyncAdminEndpoints()
        {
            var group = app.MapGroup("/admin/sync").WithTags("Sync");

            group.MapPost("/members", (SyncMembersJob job, ILogger<SyncMembersJob> logger) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await job.ExecuteAsync(new TickerFunctionContext(), CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "On-demand member sync failed.");
                    }
                });

                return Results.Accepted(value: new { status = "Started" });
            })
            .WithName("TriggerMemberSync")
            .WithSummary("Trigger an on-demand member sync");

            return app;
        }
    }
}
