using JustGo.Integrations.JustGo.Features.Clubs.Models;

namespace JustGo.Api.Features.Clubs;

public static class ClubEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapClubEndpoints()
        {
            var group = app.MapGroup("/clubs").WithTags("Clubs");

            group.MapGet("/{clubId:guid}", async (Guid clubId, IClubClient client, CancellationToken ct) =>
            {
                var result = await client.GetClubAsync(clubId, ct);
                return Results.Ok(result);
            })
            .WithName("GetClub")
            .WithSummary("Get a club by ID");

            group.MapPut("/{clubId:guid}", async (Guid clubId, ClubUpdateRequest request, IClubClient client, CancellationToken ct) =>
            {
                var result = await client.UpdateClubAsync(clubId, request, ct);
                return Results.Ok(result);
            })
            .WithName("UpdateClub")
            .WithSummary("Update a club");

            group.MapGet("/search", async (
                [AsParameters] FindClubsRequest request,
                IClubClient client,
                CancellationToken ct) =>
            {
                var result = await client.FindClubsByAttributesAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("FindClubs")
            .WithSummary("Search clubs by attributes");

            group.MapPost("/members", async (AddClubMemberRequest request, IClubClient client, CancellationToken ct) =>
            {
                var result = await client.AddClubMemberAsync(request, ct);
                return Results.Created($"/clubs/members/{result.MemberId}", result);
            })
            .WithName("AddClubMember")
            .WithSummary("Add a member to a club");

            return app;
        }
    }
}
