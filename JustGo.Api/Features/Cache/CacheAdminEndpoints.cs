using ZiggyCreatures.Caching.Fusion;

namespace JustGo.Api.Features.Cache;

public static class CacheAdminEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapCacheAdminEndpoints()
    {
        var group = app.MapGroup("/admin/cache").WithTags("Cache");

        group.MapPost("/clear", async (IFusionCache cache, CancellationToken ct) =>
        {
            await cache.ClearAsync(false, token: ct);
            return Results.Ok(new { message = "Fusion cache cleared." });
        })
        .WithName("ClearCache")
        .WithSummary("Clear FusionCache memory and distributed cache entries");

        return app;
    }
    }
}
