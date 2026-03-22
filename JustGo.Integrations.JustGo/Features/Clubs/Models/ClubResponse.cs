namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed record ClubResponse(
    Guid ClubId,
    string Name,
    string? Status,
    string? Type,
    string? Membership,
    DateTimeOffset? ModifiedDate);

public sealed record PagedClubSearchResponse(
    IReadOnlyList<ClubResponse> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
