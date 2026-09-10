using JustGo.Integrations.JustGo.Features.Organisations.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Organisations;

public sealed class OrganisationClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IOrganisationClient
{
    public Task<object> GetOrganisationAsync(Guid organisationId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Organisation/{organisationId}", ct);

    public Task UpdateOrganisationAsync(Guid organisationId, OrganisationUpdateRequest request, CancellationToken ct) =>
        PutNoContentAsync($"/api/{ApiVersion}/Organisation/{organisationId}", request, ct);

    public Task<object> SearchOrganisationsAsync(FindOrganisationsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };
        if (request.Name is not null)
        {
            query["Name"] = request.Name;
        }

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Organisation/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<object> GetRolesAsync(CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Organisation/Roles", ct);

    public Task<object> GetSchemaAsync(CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Organisation/Schema", ct);

    public Task<object> CreateOrganisationMemberAsync(Guid organisationId, OrganisationMemberCreateRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Organisation/{organisationId}/Members", request, ct);

    public Task UpdateOrganisationMemberAsync(Guid organisationId, Guid memberId, OrganisationMemberUpdateRequest request, CancellationToken ct) =>
        PutNoContentAsync($"/api/{ApiVersion}/Organisation/{organisationId}/Members/{memberId}", request, ct);

    public Task DeleteOrganisationMemberAsync(Guid organisationId, Guid memberId, CancellationToken ct) =>
        DeleteNoContentAsync($"/api/{ApiVersion}/Organisation/{organisationId}/Members/{memberId}", ct);
}
