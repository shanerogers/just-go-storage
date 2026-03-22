namespace JustGo.Integrations.JustGo.Features.Auth.Models;

public sealed record LoginResponse(string Token, string? TokenType = "Bearer");
