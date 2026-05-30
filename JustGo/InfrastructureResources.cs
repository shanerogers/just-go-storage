using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustGo;

/// <summary>
/// Extension methods that configure shared infrastructure resources
/// (database, cache) for the AppHost.
/// </summary>
internal static class InfrastructureResources
{
    public static IResourceBuilder<PostgresDatabaseResource> AddItkdDatabase(
        this IDistributedApplicationBuilder builder)
    {
        return builder.AddPostgres("db")
            .WithDbGate()
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithUrlForEndpoint("tcp", r => r.DisplayLocation = UrlDisplayLocation.DetailsOnly)
            .AddDatabase("itkd");
    }

    public static IResourceBuilder<RedisResource> AddAppCache(
        this IDistributedApplicationBuilder builder)
    {
        return builder.AddRedis("cache")
            .WithRedisInsight()
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithUrlForEndpoint("tcp", r => r.DisplayLocation = UrlDisplayLocation.DetailsOnly);
    }

    public static IResourceBuilder<RedisResource> WithClearCacheCommand(
        this IResourceBuilder<RedisResource> cache,
        IResourceBuilder<ProjectResource> api)
    {
        cache.WithHttpCommand(
            path: "/admin/cache/clear",
            displayName: "Clear Fusion Cache",
            endpointSelector: () => api.GetEndpoint("http"),
            commandName: "clear-cache",
            commandOptions: new HttpCommandOptions
            {
                IsHighlighted = true,
                IconName = "AnimalRabbitOff",
                IconVariant = IconVariant.Filled,
                UpdateState = context =>
                    context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled,
                GetCommandResult = async context =>
                {
                    var statusCode = (int)context.Response.StatusCode;

                    if (context.Response.IsSuccessStatusCode)
                    {
                        return new ExecuteCommandResult
                        {
                            Success = true,
                            Message = $"Cache cleared successfully (HTTP {statusCode})."
                        };
                    }

                    var responseBody = await context.Response.Content.ReadAsStringAsync(context.CancellationToken);
                    var detail = string.IsNullOrWhiteSpace(responseBody)
                        ? $"HTTP {statusCode} {context.Response.ReasonPhrase}"
                        : $"HTTP {statusCode}: {responseBody}";

                    return CommandResults.Failure($"Cache clear failed. {detail}");
                }
            });

        return cache;
    }
}
