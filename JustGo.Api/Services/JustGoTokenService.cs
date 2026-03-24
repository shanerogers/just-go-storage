using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace JustGo.Integrations.JustGo.Services;

public sealed class JustGoTokenService(
    IOptions<JustGoOptions> options,
    IHttpClientFactory httpClientFactory) : IJustGoTokenService
{
    private readonly JustGoOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private string? _cachedToken;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null)
        {
            return _cachedToken;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_cachedToken is not null)
            {
                return _cachedToken;
            }

            return _cachedToken = await AcquireTokenAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InvalidateToken() => _cachedToken = null;

    private async Task<string> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient("JustGoAuth");

        var response = await client.PostAsJsonAsync(
            $"/api/{_options.ApiVersion}/Auth",
            new { secret = _options.ApiKey },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken);
        var token = result?.Data?.AccessToken;

        return token ?? throw new InvalidOperationException("Token not found in JustGo authentication response.");
    }

    private sealed class ApiResponse
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("data")]
        public AuthData? Data { get; set; }
    }

    private sealed class AuthData
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("tokenType")]
        public string? TokenType { get; set; }
    }
}
