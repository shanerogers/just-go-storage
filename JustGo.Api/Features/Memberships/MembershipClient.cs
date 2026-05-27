using JustGo.Integrations.JustGo.Features.Memberships.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Memberships;

public sealed class MembershipClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IMembershipClient
{
    public Task<object> SearchDefinitionsAsync(FindMembershipDefinitionsRequest request, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Memberships/Definitions/FindByAttributes",
            new Dictionary<string, string?>
            {
                ["PageNumber"] = request.PageNumber.ToString(),
                ["PageSize"] = request.PageSize.ToString()
            });
        return GetAsync<object>(uri, ct);
    }

    public Task<object> GetDefinitionAsync(Guid definitionId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Memberships/Definitions/{definitionId}", ct);

    public Task<object> SearchMemberMembershipsAsync(FindMemberMembershipsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };
        if (request.MemberId is not null) query["memberId"] = request.MemberId.Value.ToString();

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Memberships/Member/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<object> CreateMemberMembershipAsync(Guid memberId, MemberMembershipCreateRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Memberships/Member/{memberId}", request, ct);

    public Task UpdateMembershipAsync(Guid membershipId, MembershipUpdateRequest request, CancellationToken ct) =>
        PutNoContentAsync($"/api/{ApiVersion}/Memberships/{membershipId}", request, ct);

    public Task<object> SearchClubMembershipsAsync(FindClubMembershipsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };
        if (request.ClubId is not null) query["clubId"] = request.ClubId.Value.ToString();

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Memberships/Club/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<object> CreateClubMembershipAsync(Guid clubId, ClubMembershipCreateRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Memberships/Club/{clubId}", request, ct);
}
