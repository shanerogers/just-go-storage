using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustGo;

internal static class ApiResource
{
    /// <summary>
    /// Extension methods that configure the JustGo API project resource.
    /// </summary>
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ProjectResource> AddJustGoApi(
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

            return api;
        }
    }

    extension(IResourceBuilder<ProjectResource> api)
    {
        private IResourceBuilder<ProjectResource> WithDashboardUrls()
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

        private IResourceBuilder<ProjectResource> WithSyncMembersCommand()
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
}
