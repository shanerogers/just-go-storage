using JustGo.Integrations.JustGo.Features.Credentials.Models;

namespace JustGo.Api.Features.Credentials;

public interface ICredentialClient
{
    Task<object> GetCredentialDefinitionsAsync(int pageNumber, int pageSize, CancellationToken ct);
    Task<object> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct);
    Task<object> GetCredentialDetailsAsync(CancellationToken ct);
    Task<object> FindCredentialsByAttributesAsync(FindCredentialsRequest request, CancellationToken ct);
    Task<MemberCredentialCreatedResponse> CreateMemberCredentialAsync(Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct);
    Task UpdateMemberCredentialAsync(Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct);
    Task<object> GetSchemaAsync(CancellationToken ct);
    Task<object> SearchClubCredentialsAsync(FindClubCredentialsRequest request, CancellationToken ct);
    Task<object> GetClubCredentialAsync(Guid clubId, CancellationToken ct);
    Task<object> CreateClubCredentialAsync(Guid clubId, ClubCredentialCreateRequest request, CancellationToken ct);
}
