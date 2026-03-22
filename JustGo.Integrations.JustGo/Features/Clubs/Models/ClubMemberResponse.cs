namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed record ClubMemberResponse(
    Guid? MemberId,
    Guid? ClubId,
    string? MembershipType,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? Status);
