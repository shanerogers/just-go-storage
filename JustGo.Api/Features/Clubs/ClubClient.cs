using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Clubs;

public sealed class ClubClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IClubClient
{
    private const string PageNumber = "PageNumber";
    private const string PageSize = "PageSize";

    public Task<object> GetClubAsync(Guid clubId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Clubs/{clubId}", ct);

    public Task<object> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct) =>
        PutAsync<object>($"/api/{ApiVersion}/Clubs/{clubId}", new { clubName = request.ClubName }, ct);

    public Task<object> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            [PageNumber] = request.PageNumber.ToString(),
            [PageSize] = request.PageSize.ToString()
        };
        if (request.ClubName is not null) query["ClubName"] = request.ClubName;

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Clubs/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<ClubMemberAddedResponse> AddClubMemberAsync(
        AddClubMemberRequest request, CancellationToken ct) =>
        PostAsync<ClubMemberAddedResponse>($"/api/{ApiVersion}/Clubs/AddClubMember",
            new { id = request.ClubId, member_Id = request.MemberId }, ct);
}
