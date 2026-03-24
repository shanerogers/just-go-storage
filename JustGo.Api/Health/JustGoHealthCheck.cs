using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustGo.Api.Health;

public sealed class JustGoHealthCheck(IJustGoClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var findClub = new FindClubsRequest { PageNumber = 1, PageSize = 1, ClubName = "Pilsung" };
            await client.FindClubsByAttributesAsync(findClub, cancellationToken);
            return HealthCheckResult.Healthy("JustGo API is reachable.");
        }
        catch (JustGoApiException ex)
        {
            return HealthCheckResult.Degraded(
                $"JustGo API responded with status {ex.StatusCode}.",
                data: new Dictionary<string, object> { ["statusCode"] = ex.StatusCode });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("JustGo API is unreachable.", ex);
        }
    }
}
