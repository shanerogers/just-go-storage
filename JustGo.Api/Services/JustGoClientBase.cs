using System.Text.Json;
using System.Text.Json.Serialization;
using JustGo.Api.Common;
using Microsoft.Extensions.Options;

namespace JustGo.Integrations.JustGo.Services;

/// <summary>
/// Base class for feature-specific JustGo API clients. Provides shared HTTP helpers,
/// JSON serialization options, and API version resolution.
/// </summary>
public abstract class JustGoClientBase(HttpClient httpClient, IOptions<JustGoOptions> options)
{
    protected string ApiVersion => options.Value.ApiVersion;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new LenientDateTimeOffsetConverter()
        }
    };

    protected async Task<T> GetAsync<T>(string uri, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(uri, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"Null response from GET {uri}.");
    }

    protected async Task<T> PostAsync<T>(string uri, object body, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync(uri, body, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"Null response from POST {uri}.");
    }

    protected async Task<T> PutAsync<T>(string uri, object body, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(uri, body, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"Null response from PUT {uri}.");
    }

    protected async Task PutNoContentAsync(string uri, object body, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(uri, body, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
    }

    protected async Task DeleteNoContentAsync(string uri, CancellationToken ct)
    {
        var response = await httpClient.DeleteAsync(uri, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        throw new JustGoApiException((int)response.StatusCode, body);
    }
}
