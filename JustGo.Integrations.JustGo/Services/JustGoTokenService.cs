using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JustGo.Integrations.JustGo.Services;

public interface IJustGoTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    void InvalidateToken();
}

public sealed class JustGoTokenService(
    IHttpClientFactory httpClientFactory,
    IOptions<JustGoOptions> options,
    ILogger<JustGoTokenService> logger) : IJustGoTokenService
{
    private readonly JustGoOptions _options = options.Value;
    private string? _cachedToken;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null)
            return _cachedToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null)
                return _cachedToken;

            _cachedToken = await AcquireTokenAsync(cancellationToken);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InvalidateToken() => _cachedToken = null;

    private async Task<string> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("JustGoAuth");
        var response = await client.PostAsJsonAsync(
            $"/api/{_options.ApiVersion}/Auth",
            new { secret = _options.ApiKey },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        var token = result?.Token ?? result?.AccessToken ?? result?.AccessTokenAlt;
        if (token is null)
            throw new InvalidOperationException("JustGo auth response did not contain a token.");

        logger.LogInformation("JustGo token acquired successfully.");
        return token;
    }

    private sealed class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessTokenAlt { get; set; }
    }
}
