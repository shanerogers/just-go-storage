namespace JustGo.Integrations.JustGo.Features.Auth.Models;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string Username { get; set; } = string.Empty;
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class PasswordResetRequest
{
    public string Username { get; set; } = string.Empty;
}
