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
        Url = "/quartz",
        Endpoint = endpoint,
        DisplayText = "Job Dashboard"
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
    .WaitFor(database);

cache.WithHttpCommand(
    path: "/admin/cache/clear",
    displayName: "Clear Fusion Cache",
    endpointSelector: () => api.GetEndpoint("http"),
    commandName: "clear-cache",
    commandOptions: new HttpCommandOptions
    {
        IsHighlighted = true
    });

await builder.Build().RunAsync();
