using JustGo.Integrations.JustGo.Features.Clubs.Models;

namespace JustGo.Api.Features.Clubs;

public interface IClubClient
{
    Task<object> GetClubAsync(Guid clubId, CancellationToken ct);
    Task<object> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct);
    Task<object> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct);
    Task<ClubMemberAddedResponse> AddClubMemberAsync(AddClubMemberRequest request, CancellationToken ct);
}
