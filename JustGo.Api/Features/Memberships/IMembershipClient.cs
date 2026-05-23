using JustGo.Integrations.JustGo.Features.Memberships.Models;

namespace JustGo.Api.Features.Memberships;

public interface IMembershipClient
{
    Task<object> SearchDefinitionsAsync(FindMembershipDefinitionsRequest request, CancellationToken ct);
    Task<object> GetDefinitionAsync(Guid definitionId, CancellationToken ct);
    Task<object> SearchMemberMembershipsAsync(FindMemberMembershipsRequest request, CancellationToken ct);
    Task<object> CreateMemberMembershipAsync(Guid memberId, MemberMembershipCreateRequest request, CancellationToken ct);
    Task UpdateMembershipAsync(Guid membershipId, MembershipUpdateRequest request, CancellationToken ct);
    Task<object> SearchClubMembershipsAsync(FindClubMembershipsRequest request, CancellationToken ct);
    Task<object> CreateClubMembershipAsync(Guid clubId, ClubMembershipCreateRequest request, CancellationToken ct);
}
