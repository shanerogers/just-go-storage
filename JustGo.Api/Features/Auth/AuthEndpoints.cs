using JustGo.Integrations.JustGo.Features.Auth.Models;

namespace JustGo.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, IAuthClient client, CancellationToken ct) =>
        {
            var result = await client.AuthenticateAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("Login")
        .WithSummary("Authenticate with JustGo and retrieve a bearer token");

        return app;
    }
}
