using System.Net.Http.Headers;

namespace JustGo.Integrations.JustGo.Services;

/// <summary>
/// Delegating handler that injects a JustGo bearer token into every outgoing request,
/// obtaining and caching it via <see cref="IJustGoTokenService"/>.
/// </summary>
internal sealed class JustGoAuthHandler(IJustGoTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            tokenService.InvalidateToken();
            token = await tokenService.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}

