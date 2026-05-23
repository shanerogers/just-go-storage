using JustGo.Integrations.JustGo.Features.Competitions.Models;

namespace JustGo.Api.Features.Competitions;

public interface ICompetitionClient
{
    Task<object> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct);
    Task<object> GetRankingsAsync(RankingsRequest request, CancellationToken ct);
}
