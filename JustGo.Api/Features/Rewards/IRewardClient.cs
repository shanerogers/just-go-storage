namespace JustGo.Api.Features.Rewards;

public interface IRewardClient
{
    Task<object> GetUserLinkAsync(Guid memberId, CancellationToken ct);
}
