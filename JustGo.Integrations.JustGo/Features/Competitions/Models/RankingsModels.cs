namespace JustGo.Integrations.JustGo.Features.Competitions.Models;

public sealed record RankingsRequest(
    Guid? MemberId,
    string? Category,
    string? Discipline,
    int? Year);

public sealed record RankingsResponse(
    Guid? MemberId,
    string? Category,
    string? Discipline,
    int? Ranking,
    decimal? Rating,
    int? Year);
