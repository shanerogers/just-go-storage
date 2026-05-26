using System.Net.Http.Headers;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Services;

internal sealed class JustGoAuthHandler(IJustGoTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await EnsureRequestAuthenticatedAsync(request, ct);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }

        tokenService.InvalidateToken();

        await EnsureRequestAuthenticatedAsync(request, ct);

        return await base.SendAsync(request, ct);
    }

    private async Task EnsureRequestAuthenticatedAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokenService.GetTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
