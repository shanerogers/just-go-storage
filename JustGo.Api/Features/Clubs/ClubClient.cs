using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Clubs;

// NOTE: JustGo renamed the "Clubs" resource to "Organisations" starting in API v2.2 (this app's
// configured ApiVersion). The old /Clubs/* routes no longer exist at v2.2 and return 405. The
// routes below target the real v2.2 paths/field names, confirmed against JustGo's own
// swagger.json for v2.2 (tag "Organisations", response DTOs still internally named ClubDto*).
// v2.2 also dropped the old "Club Name" search filter entirely, so name-based search is no
// longer supported by the upstream API at this version.
public sealed class ClubClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IClubClient
{
    public Task<object> GetClubAsync(Guid clubId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Organisations/{clubId}", ct);

    public Task<object> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct) =>
        PutAsync<object>($"/api/{ApiVersion}/Organisations/{clubId}", new { organisationName = request.ClubName }, ct);

    public Task<object> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Organisations/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<ClubMemberAddedResponse> AddClubMemberAsync(AddClubMemberRequest request, CancellationToken ct) =>
        PostAsync<ClubMemberAddedResponse>($"/api/{ApiVersion}/Organisations/AddMember",
            new { organisationId = request.ClubId, memberId = request.MemberId }, ct);
}
