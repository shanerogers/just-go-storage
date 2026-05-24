using System.Net.Http.Headers;

namespace JustGo.Integrations.JustGo.Services;

/// <summary>
/// Delegating handler that injects a JustGo bearer token into every outgoing request,
/// obtaining and caching it via <see cref="IJustGoTokenService"/>.
/// If the request already carries a Bearer token (e.g. forwarded from the caller),
/// that token is tried first; the handler only falls back to the token service on a 401.
/// </summary>
internal sealed class JustGoAuthHandler(IJustGoTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var hasExistingToken = request.Headers.Authorization?.Scheme is "Bearer"
            && !string.IsNullOrWhiteSpace(request.Headers.Authorization.Parameter);

        if (!hasExistingToken)
        {
            var token = await tokenService.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // First attempt returned 401 — force a fresh token regardless of what was used.
        tokenService.InvalidateToken();
        var freshToken = await tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);

        return await base.SendAsync(request, cancellationToken);
    }
}

