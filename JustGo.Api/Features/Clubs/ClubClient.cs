using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Services;
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
        var body = new Dictionary<string, object?>
        {
            [PageNumber] = request.PageNumber,
            [PageSize] = request.PageSize
        };
        if (request.ClubName is not null) body["ClubName"] = request.ClubName;

        return PostAsync<object>($"/api/{ApiVersion}/Clubs/FindByAttributes", body, ct);
    }

    public Task<ClubMemberAddedResponse> AddClubMemberAsync(
        AddClubMemberRequest request, CancellationToken ct) =>
        PostAsync<ClubMemberAddedResponse>($"/api/{ApiVersion}/Clubs/AddClubMember",
            new { id = request.ClubId, member_Id = request.MemberId }, ct);
}
