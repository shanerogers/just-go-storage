namespace JustGo.Integrations.JustGo.Features.Organisations.Models;

public sealed class FindOrganisationsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
}

public sealed class OrganisationUpdateRequest
{
    public string? Name { get; set; }
}

public sealed class OrganisationMemberCreateRequest
{
    public Guid MemberId { get; set; }
    public Guid RoleId { get; set; }
}

public sealed class OrganisationMemberUpdateRequest
{
    public Guid RoleId { get; set; }
}

public sealed class OrganisationMemberDeleteRequest
{
    public Guid MemberId { get; set; }
}
