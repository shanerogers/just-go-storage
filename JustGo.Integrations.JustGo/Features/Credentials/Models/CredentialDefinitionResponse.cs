namespace JustGo.Integrations.JustGo.Features.Credentials.Models;

public sealed record CredentialDefinitionResponse(
    Guid CredentialId,
    string? Name,
    string? Category,
    string? Description,
    bool? IsActive);

public sealed record PagedCredentialDefinitionResponse(
    IReadOnlyList<CredentialDefinitionResponse> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CredentialDetailsResponse(
    string? Message,
    IReadOnlyList<CredentialDetailItem>? Details);

public sealed record CredentialDetailItem(
    string? Key,
    string? Value);
