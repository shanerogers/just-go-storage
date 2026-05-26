using System.Net.Http.Headers;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Health;

public sealed class JustGoHealthCheck(
    IJustGoTokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IOptions<JustGoOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var token = await tokenService.GetTokenAsync(ct);
            if (string.IsNullOrWhiteSpace(token))
            {
                return HealthCheckResult.Unhealthy("JustGo token acquisition returned an empty token.");
            }

            var justGoOptions = options.Value;
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(justGoOptions.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(
                $"api/{justGoOptions.ApiVersion}/Members/FindByAttributes?PageNumber=1&PageSize=1",
                ct);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return HealthCheckResult.Unhealthy(
                $"JustGo health probe failed with {(int)response.StatusCode} ({response.StatusCode}). Body: {responseBody}");
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
