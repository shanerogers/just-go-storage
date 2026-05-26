using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace JustGo.Integrations.JustGo.Services;

public sealed class JustGoTokenService(
    IOptions<JustGoOptions> options, IHttpClientFactory httpClientFactory, IFusionCache cache) : IJustGoTokenService
{
    private const string TokenCacheKey = "justgo:auth:token";
    private static readonly TimeSpan _tokenExpirySkew = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _minimumTokenCacheDuration = TimeSpan.FromSeconds(1);

    private readonly IFusionCache _cache = cache;
    private readonly JustGoOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrSetAsync<string>(TokenCacheKey,
            async (context, cancellationToken) =>
            {
                var tokenResult = await AcquireTokenAsync(cancellationToken);
                var cacheFor = GetTokenDuration(tokenResult.ExpiresIn);
                context.Options.Duration = cacheFor;
                context.Options.DistributedCacheDuration = cacheFor;
                return tokenResult.AccessToken;
            },
            options: new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30),
                DistributedCacheDuration = TimeSpan.FromMinutes(30),
            },
            token: cancellationToken);
    }

    public void InvalidateToken() => _cache.Remove(TokenCacheKey);

    private async Task<TokenResult> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient("JustGoAuth");

        var response = await client.PostAsJsonAsync($"/api/{_options.ApiVersion}/Auth",
            new { secret = _options.ApiKey },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken);

        var token = result?.Data?.AccessToken;
        var expiresIn = result?.Data?.ExpiresIn ?? 0;

        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new TokenResult(token, expiresIn);
    }

    private static TimeSpan GetTokenDuration(int expiresInSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expiresInSeconds);
        var tokenLifetime = TimeSpan.FromSeconds(expiresInSeconds);
        return tokenLifetime > _tokenExpirySkew
            ? tokenLifetime - _tokenExpirySkew
            : _minimumTokenCacheDuration;
    }

    private readonly record struct TokenResult(string AccessToken, int ExpiresIn);

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
