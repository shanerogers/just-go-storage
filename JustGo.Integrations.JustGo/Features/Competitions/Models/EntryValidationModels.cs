namespace JustGo.Integrations.JustGo.Features.Competitions.Models;

public sealed record EntryValidationRequest(
    Guid? MemberId,
    Guid? EventId,
    string? Category,
    string? SubCategory,
    IReadOnlyList<string>? AdditionalFields);

public sealed record EntryValidationResponse(
    bool IsValid,
    string? Message,
    IReadOnlyList<string>? Errors);
