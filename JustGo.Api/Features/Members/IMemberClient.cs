namespace JustGo.Api.Features.Members;

public interface IMemberClient
{
    Task<MembersPagedResponse> FindMembersByAttributesAsync(FindMembersRequest request, CancellationToken ct);
    Task<MemberDetailDto> GetMemberAsync(Guid memberId, CancellationToken ct);
}
