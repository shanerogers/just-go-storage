namespace JustGo.Integrations.JustGo.Features.Events.Models;

public sealed record EventResponse(
    Guid EventId,
    string? Name,
    string? Category,
    string? SubCategory,
    string? Status,
    string? EventReference,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? ModifiedDate);

public sealed record PagedEventResponse(
    IReadOnlyList<EventResponse> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record FindEventsRequest(
    string? Category = null,
    string? SubCategory = null,
    string? Status = null,
    DateTimeOffset? ModificationDate = null,
    string? EventReference = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record EventCreateRequest(
    string Name,
    string? Category,
    string? SubCategory,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? Description,
    IDictionary<string, object>? AdditionalFields);

public sealed record EventCreateResponse(Guid EventId, string? Name);

public sealed record EventUpdateRequest(
    string? Name,
    string? Category,
    string? SubCategory,
    string? Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? Description);

public sealed record EventTicketResponse(
    Guid TicketId,
    string? TicketType,
    decimal? Price,
    int? Quantity,
    int? Remaining);

public sealed record PagedEventTicketResponse(
    IReadOnlyList<EventTicketResponse> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
