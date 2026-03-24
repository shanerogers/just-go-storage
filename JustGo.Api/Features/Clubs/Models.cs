namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed class FindClubsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
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
