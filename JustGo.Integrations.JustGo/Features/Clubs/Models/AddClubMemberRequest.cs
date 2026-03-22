namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed record AddClubMemberRequest(
    Guid MemberId,
    Guid ClubId,
    string? MembershipType,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);
