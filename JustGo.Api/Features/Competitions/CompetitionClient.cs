using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Competitions;

public sealed class CompetitionClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), ICompetitionClient
{
    public Task<object> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Competitions/EntryValidation",
            new { memberId = request.MemberId, eventId = request.EventId }, ct);

    public Task<object> GetRankingsAsync(RankingsRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Competitions/Rankings",
            new { memberId = request.MemberId.ToString() }, ct);
}
