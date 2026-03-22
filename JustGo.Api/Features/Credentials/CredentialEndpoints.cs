using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Credentials;

public static class CredentialEndpoints
{
    public static IEndpointRouteBuilder MapCredentialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/credentials").WithTags("Credentials");

        group.MapGet("/definitions", async (int pageNumber, int pageSize, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetCredentialDefinitionsAsync(pageNumber, pageSize, ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialDefinitions")
        .WithSummary("Get all credential definitions");

        group.MapGet("/definitions/{credentialId:guid}", async (Guid credentialId, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetCredentialDefinitionByIdAsync(credentialId, ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialDefinitionById")
        .WithSummary("Get a credential definition by ID");

        group.MapGet("/details", async (IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetCredentialDetailsAsync(ct);
            return Results.Ok(result);
        })
        .WithName("GetCredentialDetails")
        .WithSummary("Get credential detail metadata");

        group.MapGet("/search", async (
            [AsParameters] FindCredentialsRequest request,
            IJustGoClient client,
            CancellationToken ct) =>
        {
            var result = await client.FindCredentialsByAttributesAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("FindCredentials")
        .WithSummary("Search member credentials by attributes");

        group.MapPost("/{memberId:guid}", async (Guid memberId, MemberCredentialCreateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.CreateMemberCredentialAsync(memberId, request, ct);
            return Results.Created($"/credentials/{result.CredentialId}", result);
        })
        .WithName("CreateMemberCredential")
        .WithSummary("Create a credential for a member");

        group.MapPut("/{credentialId:guid}", async (Guid credentialId, MemberCredentialUpdateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            await client.UpdateMemberCredentialAsync(credentialId, request, ct);
            return Results.NoContent();
        })
        .WithName("UpdateMemberCredential")
        .WithSummary("Update a member credential");

        return app;
    }
}
