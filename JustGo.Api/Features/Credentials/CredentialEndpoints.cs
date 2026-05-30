using JustGo.Integrations.JustGo.Features.Credentials.Models;

namespace JustGo.Api.Features.Credentials;

public static class CredentialEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapCredentialEndpoints()
    {
        var group = app.MapGroup("/credentials").WithTags("Credentials");

        group.MapGet("/definitions", async (int pageNumber, int pageSize, ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.GetCredentialDefinitionsAsync(pageNumber, pageSize, ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialDefinitions")
        .WithSummary("Get all credential definitions");

        group.MapGet("/definitions/{credentialId:guid}", async (Guid credentialId, ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.GetCredentialDefinitionByIdAsync(credentialId, ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialDefinitionById")
        .WithSummary("Get a credential definition by ID");

        group.MapGet("/details", async (ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.GetCredentialDetailsAsync(ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialDetails")
        .WithSummary("Get credential detail metadata");

        group.MapGet("/search", async (
            [AsParameters] FindCredentialsRequest request,
            ICredentialClient client,
            CancellationToken ct) =>
        {
            var result = await client.FindCredentialsByAttributesAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("FindCredentials")
        .WithSummary("Search member credentials by attributes");

        group.MapPost("/{memberId:guid}", async (Guid memberId, MemberCredentialCreateRequest request, ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.CreateMemberCredentialAsync(memberId, request, ct);
            return Results.Created($"/credentials/{result.CredentialId}", result);
        })
        .WithName("CreateMemberCredential")
        .WithSummary("Create a credential for a member");

        group.MapPut("/{credentialId:guid}", async (Guid credentialId, MemberCredentialUpdateRequest request, ICredentialClient client, CancellationToken ct) =>
        {
            await client.UpdateMemberCredentialAsync(credentialId, request, ct);
            return Results.NoContent();
        })
        .WithName("UpdateMemberCredential")
        .WithSummary("Update a member credential");

        group.MapGet("/schema", async (ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.GetSchemaAsync(ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialSchema")
        .WithSummary("Get the credential data schema");

        group.MapGet("/club/search", async (
            [AsParameters] FindClubCredentialsRequest request,
            ICredentialClient client,
            CancellationToken ct) =>
        {
            var result = await client.SearchClubCredentialsAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("SearchClubCredentials")
        .WithSummary("Search club credentials by attributes");

        group.MapGet("/club/{clubId:guid}", async (Guid clubId, ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.GetClubCredentialAsync(clubId, ct);
            return Results.Ok(result);
        })
        .WithName("GetClubCredential")
        .WithSummary("Get a club credential by club ID");

        group.MapPost("/club/{clubId:guid}", async (Guid clubId, ClubCredentialCreateRequest request, ICredentialClient client, CancellationToken ct) =>
        {
            var result = await client.CreateClubCredentialAsync(clubId, request, ct);
            return Results.Created($"/credentials/club/{clubId}", result);
        })
        .WithName("CreateClubCredential")
        .WithSummary("Create a credential for a club");

        return app;
    }
    }
}
