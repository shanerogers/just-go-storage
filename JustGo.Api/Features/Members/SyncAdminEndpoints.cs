using TickerQ.Utilities.Base;

namespace JustGo.Api.Features.Members;

public static class SyncAdminEndpoints
{
    public static IEndpointRouteBuilder MapSyncAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/sync").WithTags("Sync");

        group.MapPost("/members", async (SyncMembersJob job, CancellationToken ct) =>
        {
            await job.ExecuteAsync(new TickerFunctionContext(), ct);
            return Results.Ok(new { status = "Completed" });
        })
        .WithName("TriggerMemberSync")
        .WithSummary("Trigger an on-demand member sync");

        return app;
    }
}
