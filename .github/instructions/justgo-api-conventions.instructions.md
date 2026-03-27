---
description: "Use when adding or updating JustGo.Api endpoints, feature models, API service wiring, EF Core mappings, or background sync jobs. Enforces this repository's Minimal API structure and conventions."
name: "JustGo API Conventions"
applyTo: "JustGo.Api/**/*.cs"
---
# JustGo API Conventions

Treat these as preferred defaults. If a feature requires a different approach, keep changes intentional and consistent.

- Organize API changes by feature under `Features/<FeatureName>/`.
- Put endpoint mappings in `*Endpoints.cs` static classes and expose `Map<Feature>Endpoints(this IEndpointRouteBuilder app)`.
- Map all feature endpoint groups from `Program.cs`; keep mapping calls together in the endpoint registration section.
- Keep endpoint handlers thin: orchestrate inputs, call domain/integration services (for example `IJustGoClient`), and return HTTP results.
- Use async handlers and pass `CancellationToken` through to all async service/client/EF calls.
- For endpoint metadata, add `.WithName(...)` and `.WithSummary(...)` on public endpoints.
- Prefer Minimal API result helpers (`Results.Ok`, `Results.Created`, `Results.NotFound`, etc.) over custom ad hoc response shapes.
- Keep exception handling centralized via ProblemDetails/exception middleware; avoid broad `try/catch` in endpoints unless translating a known integration exception.
- Register service dependencies in `Program.cs` alongside existing grouped registrations (options, clients, Quartz, health checks).
- For EF Core model configuration in `ApiDbContext`, follow existing snake_case table/column naming and explicit key/index configuration patterns.
- For Quartz jobs, keep scheduling and trigger configuration in `Program.cs` and job logic in feature-specific classes.

## Example shape

```csharp
public static class ClubEndpoints
{
    public static IEndpointRouteBuilder MapClubEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clubs").WithTags("Clubs");

        group.MapGet("/{clubId:guid}", async (Guid clubId, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetClubAsync(clubId, ct);
            return Results.Ok(result);
        })
        .WithName("GetClub")
        .WithSummary("Get a club by ID");

        return app;
    }
}
```
