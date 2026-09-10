namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed class FindClubsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Not sent upstream: JustGo's v2.2 Organisations/FindByAttributes endpoint dropped
    /// name-based search entirely. Kept on the request contract for backward compatibility
    /// with existing callers, but currently has no effect.
    /// </summary>
    public string? ClubName { get; set; }
}

public sealed class ClubUpdateRequest
{
    public string? ClubName { get; set; }
}

public sealed class AddClubMemberRequest
{
    public Guid ClubId { get; set; }
    public Guid MemberId { get; set; }
}

public sealed class ClubMemberAddedResponse
{
    public Guid MemberId { get; set; } = Guid.NewGuid();
}
