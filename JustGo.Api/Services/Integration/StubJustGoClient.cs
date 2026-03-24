using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Auth.Models;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Integrations.JustGo.Services;

public sealed class StubJustGoClient : IJustGoClient
{
    public Task<MembersPagedResponse> FindMembersByAttributesAsync(FindMembersRequest request, CancellationToken ct) =>
        Task.FromResult(new MembersPagedResponse
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = 0,
            TotalRecords = 0,
            Data = []
        });

    public Task<object> AuthenticateAsync(LoginRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { token = "stub-token", username = request.Username });

    public Task<object> GetClubAsync(Guid clubId, CancellationToken ct) =>
        Task.FromResult<object>(new { clubId, name = "Stub Club" });

    public Task<object> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { clubId, name = request.ClubName ?? "Stub Club" });

    public Task<object> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { request.PageNumber, request.PageSize, request.ClubName, data = Array.Empty<object>() });

    public Task<ClubMemberAddedResponse> AddClubMemberAsync(AddClubMemberRequest request, CancellationToken ct) =>
        Task.FromResult(new ClubMemberAddedResponse { MemberId = request.MemberId });

    public Task<object> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { valid = true, request.MemberId, request.EventId });

    public Task<object> GetRankingsAsync(RankingsRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { request.MemberId, rankings = Array.Empty<object>() });

    public Task<object> GetCredentialDefinitionsAsync(int pageNumber, int pageSize, CancellationToken ct) =>
        Task.FromResult<object>(new { pageNumber, pageSize, data = Array.Empty<object>() });

    public Task<object> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct) =>
        Task.FromResult<object>(new { credentialId, name = "Stub Credential" });

    public Task<object> GetCredentialDetailsAsync(CancellationToken ct) =>
        Task.FromResult<object>(new { data = Array.Empty<object>() });

    public Task<object> FindCredentialsByAttributesAsync(FindCredentialsRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { request.PageNumber, request.PageSize, request.MemberId, data = Array.Empty<object>() });

    public Task<MemberCredentialCreatedResponse> CreateMemberCredentialAsync(Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct) =>
        Task.FromResult(new MemberCredentialCreatedResponse());

    public Task UpdateMemberCredentialAsync(Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<object> FindEventsByAttributesAsync(FindEventsRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { request.PageNumber, request.PageSize, request.Name, data = Array.Empty<object>() });

    public Task<object> GetEventAsync(Guid eventId, CancellationToken ct) =>
        Task.FromResult<object>(new { eventId, name = "Stub Event" });

    public Task<EventCreatedResponse> CreateEventAsync(EventCreateRequest request, Guid? templateId, CancellationToken ct) =>
        Task.FromResult(new EventCreatedResponse());

    public Task UpdateEventAsync(Guid eventId, EventUpdateRequest request, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<object> GetEventTicketsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct) =>
        Task.FromResult<object>(new { eventId, pageNumber, pageSize, data = Array.Empty<object>() });

    public Task<object> FindEventCandidatesByAttributesAsync(FindEventCandidatesRequest request, CancellationToken ct) =>
        Task.FromResult<object>(new { request.PageNumber, request.PageSize, request.EventId, data = Array.Empty<object>() });

    public Task<EventCandidateCreatedResponse> AddEventCandidateAsync(Guid eventId, EventCandidateCreateRequest request, CancellationToken ct) =>
        Task.FromResult(new EventCandidateCreatedResponse());

    public Task UpdateEventCandidateStatusAsync(Guid bookingId, EventCandidateStatusUpdateRequest request, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<object> GetEventPromotersAsync(Guid eventId, CancellationToken ct) =>
        Task.FromResult<object>(new { eventId, data = Array.Empty<object>() });

    public Task<EventPromoterCreatedResponse> AddEventPromoterAsync(Guid eventId, EventPromoterCreateRequest request, CancellationToken ct) =>
        Task.FromResult(new EventPromoterCreatedResponse { PromoterId = request.PromoterId });

    public Task RemoveEventPromoterAsync(Guid eventId, int promoterId, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<object> GetEventStagesAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct) =>
        Task.FromResult<object>(new { eventId, pageNumber, pageSize, data = Array.Empty<object>() });

    public Task<EventStageCreatedResponse> CreateEventStageAsync(Guid eventId, EventStageCreateRequest request, CancellationToken ct) =>
        Task.FromResult(new EventStageCreatedResponse());
}
