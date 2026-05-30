using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustGo;

/// <summary>
/// Extension methods that configure the JustGo API project resource.
/// </summary>
internal static class ApiResource
{
    public static IResourceBuilder<ProjectResource> AddJustGoApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<RedisResource> cache,
        IResourceBuilder<ParameterResource> apiKey)
    {
        var api = builder.AddProject<Projects.JustGo_Api>("api")
            .WithHttpHealthCheck("/health")
            .WithDashboardUrls()
            .WithUrlForEndpoint("https", r => r.DisplayLocation = UrlDisplayLocation.DetailsOnly)
            .WithEnvironment("JustGo__ApiKey", apiKey)
            .WithReference(cache)
            .WithReference(database)
            .WaitFor(cache)
            .WaitFor(database)
            .WithSyncMembersCommand();

        cache.WithClearCacheCommand(api);

        return api;
    }

    private static IResourceBuilder<ProjectResource> WithDashboardUrls(
        this IResourceBuilder<ProjectResource> api)
    {
        return api
            .WithUrlForEndpoint("http", endpoint => new()
            {
                DisplayOrder = 1,
                Url = "/tickerq/dashboard",
                Endpoint = endpoint,
                DisplayText = "TickerQ Dashboard"
            })
            .WithUrlForEndpoint("http", r =>
            {
                r.Url = "/health-ui";
                r.DisplayText = "Health Dashboard";
            });
    }

    private static IResourceBuilder<ProjectResource> WithSyncMembersCommand(
        this IResourceBuilder<ProjectResource> api)
    {
        return api.WithHttpCommand(
            path: "/admin/sync/members",
            displayName: "Sync Members",
            commandOptions: new HttpCommandOptions
            {
                Method = HttpMethod.Post,
                Description = "Trigger an on-demand member sync job",
                IconName = "PeopleSync",
                IconVariant = IconVariant.Filled,
                IsHighlighted = true,
                UpdateState = context =>
                    context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled,
                ResultMode = HttpCommandResultMode.Auto
            });
    }
}
