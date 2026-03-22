namespace JustGo.Integrations.JustGo.Features.Events.Models;

public sealed record EventStageResponse(
    Guid StageId,
    Guid EventId,
    string? Name,
    int? Order,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);

public sealed record EventStageCreateRequest(
    string Name,
    int? Order,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);
