namespace JustGo.Integrations.JustGo.Features.Memberships.Models;

public sealed class FindMembershipDefinitionsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class FindMemberMembershipsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? MemberId { get; set; }
}

public sealed class FindClubMembershipsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? ClubId { get; set; }
}

public sealed class MemberMembershipCreateRequest
{
    public Guid? MembershipDefinitionId { get; set; }
}

public sealed class ClubMembershipCreateRequest
{
    public Guid? MembershipDefinitionId { get; set; }
}

public sealed class MembershipUpdateRequest
{
    public string? Status { get; set; }
}
