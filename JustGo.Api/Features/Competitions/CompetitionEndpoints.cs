using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Competitions;

public static class CompetitionEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competitions").WithTags("Competitions");

        group.MapPost("/entry-validation", async (EntryValidationRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.ValidateEntryAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("ValidateEntry")
        .WithSummary("Validate a competition entry");

        group.MapPost("/rankings", async (RankingsRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetRankingsAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("GetRankings")
        .WithSummary("Get competition rankings for a member");

        return app;
    }
}
