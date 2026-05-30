using JustGo.Integrations.JustGo.Features.Memberships.Models;

namespace JustGo.Api.Features.Memberships;

public static class MembershipEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapMembershipEndpoints()
        {
            var group = app.MapGroup("/memberships").WithTags("Memberships");

            group.MapGet("/definitions/search", async (
                [AsParameters] FindMembershipDefinitionsRequest request,
                IMembershipClient client,
                CancellationToken ct) =>
            {
                var result = await client.SearchDefinitionsAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("SearchMembershipDefinitions")
            .WithSummary("Search membership definitions");

            group.MapGet("/definitions/{definitionId:guid}", async (Guid definitionId, IMembershipClient client, CancellationToken ct) =>
            {
                var result = await client.GetDefinitionAsync(definitionId, ct);
                return Results.Ok(result);
            })
            .WithName("GetMembershipDefinition")
            .WithSummary("Get a membership definition by ID");

            group.MapGet("/member/search", async (
                [AsParameters] FindMemberMembershipsRequest request,
                IMembershipClient client,
                CancellationToken ct) =>
            {
                var result = await client.SearchMemberMembershipsAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("SearchMemberMemberships")
            .WithSummary("Search memberships for a member");

            group.MapPost("/member/{memberId:guid}", async (Guid memberId, MemberMembershipCreateRequest request, IMembershipClient client, CancellationToken ct) =>
            {
                var result = await client.CreateMemberMembershipAsync(memberId, request, ct);
                return Results.Created($"/memberships/member/{memberId}", result);
            })
            .WithName("CreateMemberMembership")
            .WithSummary("Create a membership for a member");

            group.MapPut("/{membershipId:guid}", async (Guid membershipId, MembershipUpdateRequest request, IMembershipClient client, CancellationToken ct) =>
            {
                await client.UpdateMembershipAsync(membershipId, request, ct);
                return Results.NoContent();
            })
            .WithName("UpdateMembership")
            .WithSummary("Update a membership");

            group.MapGet("/club/search", async (
                [AsParameters] FindClubMembershipsRequest request,
                IMembershipClient client,
                CancellationToken ct) =>
            {
                var result = await client.SearchClubMembershipsAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("SearchClubMemberships")
            .WithSummary("Search memberships for a club");

            group.MapPost("/club/{clubId:guid}", async (Guid clubId, ClubMembershipCreateRequest request, IMembershipClient client, CancellationToken ct) =>
            {
                var result = await client.CreateClubMembershipAsync(clubId, request, ct);
                return Results.Created($"/memberships/club/{clubId}", result);
            })
            .WithName("CreateClubMembership")
            .WithSummary("Create a membership for a club");

            return app;
        }
    }
}
