using JustGo.Integrations.JustGo.Features.Auth.Models;

namespace JustGo.Api.Features.Auth;

public interface IAuthClient
{
    Task<object> AuthenticateAsync(LoginRequest request, CancellationToken ct);
    Task<object> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
    Task<object> PasswordResetAsync(PasswordResetRequest request, CancellationToken ct);
}
