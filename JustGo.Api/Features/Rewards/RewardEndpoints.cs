namespace JustGo.Api.Features.Rewards;

public static class RewardEndpoints
{
    public static IEndpointRouteBuilder MapRewardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rewards").WithTags("Rewards");

        group.MapGet("/user-link/{memberId:guid}", async (Guid memberId, IRewardClient client, CancellationToken ct) =>
        {
            var result = await client.GetUserLinkAsync(memberId, ct);
            return Results.Ok(result);
        })
        .WithName("GetRewardUserLink")
        .WithSummary("Get the rewards user link for a member");

        return app;
    }
}
