var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DashboardApplicationName = "Just Go"
});

var database = builder.AddPostgres("db")
    .WithDbGate()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithUrlForEndpoint("tcp", resource => resource.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .AddDatabase("itkd");

var cache = builder.AddRedis("cache")
    .WithRedisInsight()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithUrlForEndpoint("tcp", resource => resource.DisplayLocation = UrlDisplayLocation.DetailsOnly);

var apiKey = builder.AddParameter("justgo-apikey", secret: true);

var api = builder.AddProject<Projects.JustGo_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", endpoint => new()
    {
        DisplayOrder = 1,
        Url = "/tickerq/dashboard",
        Endpoint = endpoint,
        DisplayText = "TickerQ Dashboard"
    })
    .WithUrlForEndpoint("http", resource =>
    {
        resource.Url = "/health-ui";
        resource.DisplayText = "Health Dashboard";
    })
    .WithUrlForEndpoint("https", resource => resource.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithEnvironment("JustGo__ApiKey", apiKey)
    .WithReference(cache)
    .WithReference(database)
    .WaitFor(cache)
    .WaitFor(database)
    .WithHttpCommand(
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
                context.ResourceSnapshot.HealthStatus is Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled,
            ResultMode = HttpCommandResultMode.Auto
        });

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
            context.ResourceSnapshot.HealthStatus is Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
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

await builder.Build().RunAsync();
