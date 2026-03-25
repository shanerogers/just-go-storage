using System.Net;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Health;

public sealed class JustGoHealthCheck(IHttpClientFactory factory, IOptions<JustGoOptions> options) : IHealthCheck
{
    private readonly IHttpClientFactory _factory = factory;
    private readonly JustGoOptions _options = options.Value;


    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var client = _factory.CreateClient();
            client.BaseAddress = new Uri(_options.BaseUrl);
            var response = await client.GetAsync("api/v2.2/Members/FindByAttributes?PageNumber=1&PageSize=10", ct);
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (JustGoApiException ex)
        {
            return HealthCheckResult.Degraded($"JustGo responded with {ex.StatusCode}.", exception: ex);
        }
        catch (OperationCanceledException ex)
        {
            return HealthCheckResult.Degraded("JustGo API health check was canceled.", exception: ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("JustGo API is unreachable.", exception: ex);
        }
    }
}
