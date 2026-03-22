namespace JustGo.Integrations.JustGo.Features.Events.Models;

public sealed record EventPromoterResponse(
    int PromoterId,
    Guid EventId,
    string? Name,
    string? ContactEmail,
    string? ContactPhone);

public sealed record EventPromoterCreateRequest(
    string Name,
    string? ContactEmail,
    string? ContactPhone);
