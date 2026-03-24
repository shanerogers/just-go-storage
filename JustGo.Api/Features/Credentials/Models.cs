namespace JustGo.Integrations.JustGo.Features.Credentials.Models;

public sealed class FindCredentialsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? MemberId { get; set; }
}

public sealed class MemberCredentialCreateRequest
{
    public string? CredentialType { get; set; }
    public string? Value { get; set; }
}

public sealed class MemberCredentialUpdateRequest
{
    public string? Value { get; set; }
}
