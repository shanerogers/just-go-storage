using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Rewards;

public sealed class RewardClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IRewardClient
{
    public Task<object> GetUserLinkAsync(Guid memberId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Rewards/UserLink/{memberId}", ct);
}
