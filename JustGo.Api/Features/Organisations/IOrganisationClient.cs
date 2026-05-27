using JustGo.Integrations.JustGo.Features.Organisations.Models;

namespace JustGo.Api.Features.Organisations;

public interface IOrganisationClient
{
    Task<object> GetOrganisationAsync(Guid organisationId, CancellationToken ct);
    Task UpdateOrganisationAsync(Guid organisationId, OrganisationUpdateRequest request, CancellationToken ct);
    Task<object> SearchOrganisationsAsync(FindOrganisationsRequest request, CancellationToken ct);
    Task<object> GetRolesAsync(CancellationToken ct);
    Task<object> GetSchemaAsync(CancellationToken ct);
    Task<object> CreateOrganisationMemberAsync(Guid organisationId, OrganisationMemberCreateRequest request, CancellationToken ct);
    Task UpdateOrganisationMemberAsync(Guid organisationId, Guid memberId, OrganisationMemberUpdateRequest request, CancellationToken ct);
    Task DeleteOrganisationMemberAsync(Guid organisationId, Guid memberId, CancellationToken ct);
}
