using JustGo.Integrations.JustGo.Features.Organisations.Models;

namespace JustGo.Api.Features.Organisations;

public static class OrganisationEndpoints
{
    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organisations").WithTags("Organisations");

        group.MapGet("/search", async (
            [AsParameters] FindOrganisationsRequest request,
            IOrganisationClient client,
            CancellationToken ct) =>
        {
            var result = await client.SearchOrganisationsAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("SearchOrganisations")
        .WithSummary("Search organisations by attributes");

        group.MapGet("/{organisationId:guid}", async (Guid organisationId, IOrganisationClient client, CancellationToken ct) =>
        {
            var result = await client.GetOrganisationAsync(organisationId, ct);
            return Results.Ok(result);
        })
        .WithName("GetOrganisation")
        .WithSummary("Get an organisation by ID");

        group.MapPut("/{organisationId:guid}", async (Guid organisationId, OrganisationUpdateRequest request, IOrganisationClient client, CancellationToken ct) =>
        {
            await client.UpdateOrganisationAsync(organisationId, request, ct);
            return Results.NoContent();
        })
        .WithName("UpdateOrganisation")
        .WithSummary("Update an organisation");

        group.MapGet("/roles", async (IOrganisationClient client, CancellationToken ct) =>
        {
            var result = await client.GetRolesAsync(ct);
            return Results.Ok(result);
        })
        .WithName("GetOrganisationRoles")
        .WithSummary("Get available organisation roles");

        group.MapGet("/schema", async (IOrganisationClient client, CancellationToken ct) =>
        {
            var result = await client.GetSchemaAsync(ct);
            return Results.Ok(result);
        })
        .WithName("GetOrganisationSchema")
        .WithSummary("Get the organisation data schema");

        group.MapPost("/{organisationId:guid}/members", async (Guid organisationId, OrganisationMemberCreateRequest request, IOrganisationClient client, CancellationToken ct) =>
        {
            var result = await client.CreateOrganisationMemberAsync(organisationId, request, ct);
            return Results.Created($"/organisations/{organisationId}/members", result);
        })
        .WithName("CreateOrganisationMember")
        .WithSummary("Add a member to an organisation");

        group.MapPut("/{organisationId:guid}/members/{memberId:guid}", async (Guid organisationId, Guid memberId, OrganisationMemberUpdateRequest request, IOrganisationClient client, CancellationToken ct) =>
        {
            await client.UpdateOrganisationMemberAsync(organisationId, memberId, request, ct);
            return Results.NoContent();
        })
        .WithName("UpdateOrganisationMember")
        .WithSummary("Update a member's role in an organisation");

        group.MapDelete("/{organisationId:guid}/members/{memberId:guid}", async (Guid organisationId, Guid memberId, IOrganisationClient client, CancellationToken ct) =>
        {
            await client.DeleteOrganisationMemberAsync(organisationId, memberId, ct);
            return Results.NoContent();
        })
        .WithName("DeleteOrganisationMember")
        .WithSummary("Remove a member from an organisation");

        return app;
    }
}
