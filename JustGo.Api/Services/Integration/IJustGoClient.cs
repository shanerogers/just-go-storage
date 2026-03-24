using JustGo.Integrations.JustGo.Features.Auth.Models;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Integrations.JustGo.Services;

public interface IJustGoClient
{
    Task<object> AuthenticateAsync(LoginRequest request, CancellationToken ct);
    Task<object> GetClubAsync(Guid clubId, CancellationToken ct);
    Task<object> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct);
    Task<object> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct);
    Task<ClubMemberAddedResponse> AddClubMemberAsync(AddClubMemberRequest request, CancellationToken ct);
    Task<object> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct);
    Task<object> GetRankingsAsync(RankingsRequest request, CancellationToken ct);
    Task<object> GetCredentialDefinitionsAsync(int pageNumber, int pageSize, CancellationToken ct);
    Task<object> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct);
    Task<object> GetCredentialDetailsAsync(CancellationToken ct);
    Task<object> FindCredentialsByAttributesAsync(FindCredentialsRequest request, CancellationToken ct);
    Task<MemberCredentialCreatedResponse> CreateMemberCredentialAsync(Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct);
    Task UpdateMemberCredentialAsync(Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct);
    Task<object> FindEventsByAttributesAsync(FindEventsRequest request, CancellationToken ct);
    Task<object> GetEventAsync(Guid eventId, CancellationToken ct);
    Task<EventCreatedResponse> CreateEventAsync(EventCreateRequest request, Guid? templateId, CancellationToken ct);
    Task UpdateEventAsync(Guid eventId, EventUpdateRequest request, CancellationToken ct);
    Task<object> GetEventTicketsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct);
    Task<object> FindEventCandidatesByAttributesAsync(FindEventCandidatesRequest request, CancellationToken ct);
    Task<EventCandidateCreatedResponse> AddEventCandidateAsync(Guid eventId, EventCandidateCreateRequest request, CancellationToken ct);
    Task UpdateEventCandidateStatusAsync(Guid bookingId, EventCandidateStatusUpdateRequest request, CancellationToken ct);
    Task<object> GetEventPromotersAsync(Guid eventId, CancellationToken ct);
    Task<EventPromoterCreatedResponse> AddEventPromoterAsync(Guid eventId, EventPromoterCreateRequest request, CancellationToken ct);
    Task RemoveEventPromoterAsync(Guid eventId, int promoterId, CancellationToken ct);
    Task<object> GetEventStagesAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct);
    Task<EventStageCreatedResponse> CreateEventStageAsync(Guid eventId, EventStageCreateRequest request, CancellationToken ct);
}

public sealed class MemberCredentialCreatedResponse
{
    public Guid CredentialId { get; set; } = Guid.NewGuid();
}

public sealed class ClubMemberAddedResponse
{
    public Guid MemberId { get; set; } = Guid.NewGuid();
}

public sealed class EventCreatedResponse
{
    public Guid EventId { get; set; } = Guid.NewGuid();
}

public sealed class EventCandidateCreatedResponse
{
    public Guid BookingId { get; set; } = Guid.NewGuid();
}

public sealed class EventPromoterCreatedResponse
{
    public int PromoterId { get; set; }
}

public sealed class EventStageCreatedResponse
{
    public Guid StageId { get; set; } = Guid.NewGuid();
}
