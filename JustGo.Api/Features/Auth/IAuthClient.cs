using JustGo.Integrations.JustGo.Features.Auth.Models;

namespace JustGo.Api.Features.Auth;

public interface IAuthClient
{
    Task<object> AuthenticateAsync(LoginRequest request, CancellationToken ct);
}
