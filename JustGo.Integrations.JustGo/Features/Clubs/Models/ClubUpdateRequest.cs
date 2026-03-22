namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed record ClubUpdateRequest(
    string? Name,
    string? Status,
    string? Type,
    string? Membership);
