namespace JustGo.Api.Features.Members;

public interface IMemberClient
{
    Task<MembersPagedResponse> FindMembersByAttributesAsync(FindMembersRequest request, CancellationToken ct);
    Task<MemberDetailDto> GetMemberAsync(Guid memberId, CancellationToken ct);
    Task UpdateMemberAsync(Guid memberId, MemberUpdateRequest request, CancellationToken ct);
    Task<MemberCreatedResponse> CreateMemberAsync(MemberCreateRequest request, CancellationToken ct);
    Task SuspendMemberAsync(MemberSuspendRequest request, CancellationToken ct);
    Task UploadProfileImageAsync(Guid memberId, Stream image, string fileName, CancellationToken ct);
    Task<object> GetSchemaAsync(CancellationToken ct);
}
