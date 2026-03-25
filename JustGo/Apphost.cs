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

var apiKey = builder.AddParameter("justgo-apikey", secret: true);

builder.AddProject<Projects.JustGo_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", endpoint => new()
    {
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
    .WithReference(database)
    .WaitFor(database);

await builder.Build().RunAsync();
