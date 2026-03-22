namespace JustGo.Integrations.JustGo.Features.Clubs.Models;

public sealed record FindClubsRequest(
    string? Status = null,
    string? Type = null,
    Guid? CredentialId = null,
    string? Membership = null,
    string? ClubName = null,
    string? ClubId = null,
    DateTimeOffset? ModifiedDate = null,
    int PageNumber = 1,
    int PageSize = 20);
