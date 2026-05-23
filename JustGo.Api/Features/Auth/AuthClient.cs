using JustGo.Integrations.JustGo.Features.Auth.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Auth;

public sealed class AuthClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IAuthClient
{
    public Task<object> AuthenticateAsync(LoginRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Members/LogInCheck",
            new { userName = request.Username, password = request.Password }, ct);
}
