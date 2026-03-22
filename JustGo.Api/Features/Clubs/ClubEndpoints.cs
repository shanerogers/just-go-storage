using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Clubs;

public static class ClubEndpoints
{
    public static IEndpointRouteBuilder MapClubEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clubs").WithTags("Clubs");

        group.MapGet("/{clubId:guid}", async (Guid clubId, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetClubAsync(clubId, ct);
            return Results.Ok(result);
        })
        .WithName("GetClub")
        .WithSummary("Get a club by ID");

        group.MapPut("/{clubId:guid}", async (Guid clubId, ClubUpdateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.UpdateClubAsync(clubId, request, ct);
            return Results.Ok(result);
        })
        .WithName("UpdateClub")
        .WithSummary("Update a club");

        group.MapGet("/search", async (
            [AsParameters] FindClubsRequest request,
            IJustGoClient client,
            CancellationToken ct) =>
        {
            var result = await client.FindClubsByAttributesAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("FindClubs")
        .WithSummary("Search clubs by attributes");

        group.MapPost("/members", async (AddClubMemberRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.AddClubMemberAsync(request, ct);
            return Results.Created($"/clubs/members/{result.MemberId}", result);
        })
        .WithName("AddClubMember")
        .WithSummary("Add a member to a club");

        return app;
    }
}
