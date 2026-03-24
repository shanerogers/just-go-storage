namespace JustGo.Integrations.JustGo.Features.Events.Models;

public sealed class FindEventsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
}

public sealed class EventCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class EventUpdateRequest
{
    public string? Name { get; set; }
}

public sealed class FindEventCandidatesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? EventId { get; set; }
}

public sealed class EventCandidateCreateRequest
{
    public Guid MemberId { get; set; }
}

public sealed class EventCandidateStatusUpdateRequest
{
    public string Status { get; set; } = "Pending";
}

public sealed class EventPromoterCreateRequest
{
    public int PromoterId { get; set; }
}

public sealed class EventStageCreateRequest
{
    public string Name { get; set; } = string.Empty;
}
