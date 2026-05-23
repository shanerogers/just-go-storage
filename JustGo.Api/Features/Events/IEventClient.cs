using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Api.Features.Events;

public interface IEventClient
{
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
