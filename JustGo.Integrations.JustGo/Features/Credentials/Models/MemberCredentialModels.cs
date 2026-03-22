namespace JustGo.Integrations.JustGo.Features.Credentials.Models;

public sealed record MemberCredentialCreateRequest(
    Guid CredentialDefinitionId,
    string? Status,
    DateTimeOffset? IssueDate,
    DateTimeOffset? ExpiryDate,
    string? Notes);

public sealed record MemberCredentialUpdateRequest(
    string? Status,
    DateTimeOffset? IssueDate,
    DateTimeOffset? ExpiryDate,
    string? Notes);

public sealed record MemberCredentialResponse(
    Guid CredentialId,
    Guid MemberId,
    string? CredentialName,
    string? Status,
    DateTimeOffset? IssueDate,
    DateTimeOffset? ExpiryDate,
    string? Notes);

public sealed record PagedMemberCredentialResponse(
    IReadOnlyList<MemberCredentialResponse> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record FindCredentialsRequest(
    Guid? MemberId = null,
    string? Status = null,
    string? Category = null,
    int PageNumber = 1,
    int PageSize = 20);
