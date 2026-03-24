namespace JustGo.Integrations.JustGo.Features.Competitions.Models;

public sealed class EntryValidationRequest
{
    public Guid MemberId { get; set; }
    public Guid EventId { get; set; }
}

public sealed class RankingsRequest
{
    public Guid MemberId { get; set; }
}
