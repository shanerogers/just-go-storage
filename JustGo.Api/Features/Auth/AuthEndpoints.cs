using JustGo.Integrations.JustGo.Features.Auth.Models;

namespace JustGo.Api.Features.Auth;

public static class AuthEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapAuthEndpoints()
        {
            var group = app.MapGroup("/auth").WithTags("Auth");

            group.MapPost("/login", async (LoginRequest request, IAuthClient client, CancellationToken ct) =>
            {
                var result = await client.AuthenticateAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("Login")
            .WithSummary("Authenticate with JustGo and retrieve a bearer token");

            group.MapPost("/change-password", async (ChangePasswordRequest request, IAuthClient client, CancellationToken ct) =>
            {
                var result = await client.ChangePasswordAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("ChangePassword")
            .WithSummary("Change a member's password");

            group.MapPost("/password-reset", async (PasswordResetRequest request, IAuthClient client, CancellationToken ct) =>
            {
                var result = await client.PasswordResetAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("PasswordReset")
            .WithSummary("Request a password reset for a member");

            return app;
        }
    }
}
