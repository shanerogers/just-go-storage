using JustGo.Integrations.JustGo.Features.Auth.Models;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Integrations.JustGo.Services;

public interface IJustGoClient
{
    // Auth
    Task<LoginResponse> AuthenticateAsync(LoginRequest request, CancellationToken ct = default);

    // Clubs
    Task<ClubResponse> GetClubAsync(Guid clubId, CancellationToken ct = default);
    Task<ClubResponse> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct = default);
    Task<PagedClubSearchResponse> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct = default);
    Task<ClubMemberResponse> AddClubMemberAsync(AddClubMemberRequest request, CancellationToken ct = default);

    // Competitions
    Task<EntryValidationResponse> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct = default);
    Task<RankingsResponse> GetRankingsAsync(RankingsRequest request, CancellationToken ct = default);

    // Credentials
    Task<PagedCredentialDefinitionResponse> GetCredentialDefinitionsAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    Task<PagedMemberCredentialResponse> FindCredentialsByAttributesAsync(FindCredentialsRequest request, CancellationToken ct = default);
    Task<MemberCredentialResponse> CreateMemberCredentialAsync(Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct = default);
    Task UpdateMemberCredentialAsync(Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct = default);
    Task<PagedCredentialDefinitionResponse> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct = default);
    Task<CredentialDetailsResponse> GetCredentialDetailsAsync(CancellationToken ct = default);

    // Events
    Task<PagedEventResponse> FindEventsByAttributesAsync(FindEventsRequest request, CancellationToken ct = default);
    Task<EventResponse> GetEventAsync(Guid eventId, CancellationToken ct = default);
    Task<EventCreateResponse> CreateEventAsync(EventCreateRequest request, Guid? templateId = null, CancellationToken ct = default);
    Task UpdateEventAsync(Guid eventId, EventUpdateRequest request, CancellationToken ct = default);
    Task<PagedEventTicketResponse> GetEventTicketsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct = default);

    // Event Candidates
    Task<PagedEventCandidateResponse> FindEventCandidatesByAttributesAsync(FindEventCandidatesRequest request, CancellationToken ct = default);
    Task<EventCandidateResponse> AddEventCandidateAsync(Guid eventId, EventCandidateCreateRequest request, CancellationToken ct = default);
    Task UpdateEventCandidateStatusAsync(Guid bookingId, EventCandidateStatusUpdateRequest request, CancellationToken ct = default);

    // Event Promoters
    Task<IReadOnlyList<EventPromoterResponse>> GetEventPromotersAsync(Guid eventId, CancellationToken ct = default);
    Task<EventPromoterResponse> AddEventPromoterAsync(Guid eventId, EventPromoterCreateRequest request, CancellationToken ct = default);
    Task RemoveEventPromoterAsync(Guid eventId, int promoterId, CancellationToken ct = default);

    // Event Stages
    Task<IReadOnlyList<EventStageResponse>> GetEventStagesAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<EventStageResponse> CreateEventStageAsync(Guid eventId, EventStageCreateRequest request, CancellationToken ct = default);
}
