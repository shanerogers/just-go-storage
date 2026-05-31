using JustGo.Integrations.JustGo.Features.Events.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Events;

public sealed class EventClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IEventClient
{
    private const string PageNumber = "PageNumber";
    private const string PageSize = "PageSize";

    // ── Events ───────────────────────────────────────────────────────────────

    public Task<object> FindEventsByAttributesAsync(FindEventsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            [PageNumber] = request.PageNumber.ToString(),
            [PageSize] = request.PageSize.ToString()
        };
        if (request.Name is not null) query["EventName"] = request.Name;
        if (request.Category is not null) query["EventCategory"] = ToJustGoCategory(request.Category.Value);

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    private static string ToJustGoCategory(EventCategory category) => category switch
    {
        EventCategory.GupGrading => "Gup Grading",
        EventCategory.DanGrading => "Dan Grading",
        EventCategory.DanPassIncomplete => "Dan Pass Incomplete",
        EventCategory.Courses => "Courses",
        EventCategory.ClubEvents => "Club Events",
        EventCategory.Other => "Other",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };

    public Task<object> GetEventAsync(Guid eventId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Events/{eventId}", ct);

    public Task<EventCreatedResponse> CreateEventAsync(
        EventCreateRequest request, Guid? templateId, CancellationToken ct)
    {
        var path = templateId is not null
            ? $"/api/{ApiVersion}/Events?templateId={templateId}"
            : $"/api/{ApiVersion}/Events";
        return PostAsync<EventCreatedResponse>(path, new { eventName = request.Name }, ct);
    }

    public async Task UpdateEventAsync(Guid eventId, EventUpdateRequest request, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events",
            new Dictionary<string, string?> { ["id"] = eventId.ToString() });
        await PutNoContentAsync(uri, new { eventName = request.Name }, ct);
    }

    public Task<EventCollectionResponse<EventTicketDto>> GetEventTicketsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/{eventId}/Tickets",
            new Dictionary<string, string?>
            {
                [PageNumber] = pageNumber.ToString(),
                [PageSize] = pageSize.ToString()
            });
        return GetAsync<EventCollectionResponse<EventTicketDto>>(uri, ct);
    }

    // ── Event Candidates ──────────────────────────────────────────────────────

    public Task<EventCollectionResponse<EventCandidateDto>> FindEventCandidatesByAttributesAsync(FindEventCandidatesRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            [PageNumber] = request.PageNumber.ToString(),
            [PageSize] = request.PageSize.ToString()
        };
        if (request.EventId is not null) query["EventId"] = request.EventId.Value.ToString();

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/Candidate/FindByAttributes", query);
        return GetAsync<EventCollectionResponse<EventCandidateDto>>(uri, ct);
    }

    public Task<EventCandidateCreatedResponse> AddEventCandidateAsync(
        EventCandidateCreateRequest request, CancellationToken ct) =>
        PostAsync<EventCandidateCreatedResponse>($"/api/{ApiVersion}/Events/Candidates",
            new { candidateId = request.MemberId, ticketId = request.TicketId }, ct);

    public Task UpdateEventCandidateStatusAsync(
        Guid bookingId, EventCandidateStatusUpdateRequest request, CancellationToken ct) =>
        PutNoContentAsync($"/api/{ApiVersion}/Events/Candidates/{bookingId}", new { status = request.Status }, ct);

    // ── Event Promoters ───────────────────────────────────────────────────────

    public Task<object> GetEventPromotersAsync(Guid eventId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Events/{eventId}/Promoters", ct);

    public Task<EventPromoterCreatedResponse> AddEventPromoterAsync(
        Guid eventId, EventPromoterCreateRequest request, CancellationToken ct) =>
        PostAsync<EventPromoterCreatedResponse>($"/api/{ApiVersion}/Events/{eventId}/Promoters",
            new { promoterId = request.PromoterId }, ct);

    public Task RemoveEventPromoterAsync(Guid eventId, int promoterId, CancellationToken ct) =>
        DeleteNoContentAsync($"/api/{ApiVersion}/Events/{eventId}/Promoters/{promoterId}", ct);

    // ── Event Stages ──────────────────────────────────────────────────────────

    public Task<object> GetEventStagesAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/{eventId}/Stages",
            new Dictionary<string, string?>
            {
                [PageNumber] = pageNumber.ToString(),
                [PageSize] = pageSize.ToString()
            });
        return GetAsync<object>(uri, ct);
    }

    public Task<EventStageCreatedResponse> CreateEventStageAsync(
        Guid eventId, EventStageCreateRequest request, CancellationToken ct) =>
        PostAsync<EventStageCreatedResponse>($"/api/{ApiVersion}/Events/{eventId}/Stages",
            new { fixtureName = request.Name }, ct);

    public Task DeleteEventStageAsync(Guid eventId, Guid stageId, CancellationToken ct) =>
        DeleteNoContentAsync($"/api/{ApiVersion}/Events/{eventId}/Stages/{stageId}", ct);

    public Task<object> GetSchemaAsync(CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Events/Schema", ct);
}
