namespace JustGo.Integrations.JustGo.Features.Events.Models;

public sealed record EventCandidateResponse(
    Guid BookingId,
    Guid EventId,
    Guid MemberId,
    string? BookingStatus,
    string? Category,
    string? SubCategory,
    DateTimeOffset? ModifiedDate);

public sealed record PagedEventCandidateResponse(
    IReadOnlyList<EventCandidateResponse> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record FindEventCandidatesRequest(
    string? BookingStatus = null,
    Guid? EventId = null,
    string? Category = null,
    string? SubCategory = null,
    DateTimeOffset? ModificationDate = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record EventCandidateCreateRequest(
    Guid MemberId,
    string? Category,
    string? SubCategory,
    IDictionary<string, object>? AdditionalFields);

public sealed record EventCandidateStatusUpdateRequest(string BookingStatus);
