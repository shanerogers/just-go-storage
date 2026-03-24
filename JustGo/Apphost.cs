var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("justgo-db")
    .WithDbGate()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("tkd-nz");

var apiKey = builder.AddParameter("justgo-apikey", secret: true);

builder.AddProject<Projects.JustGo_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", endpoint => new()
    {
        Url = "/quartz",
        DisplayText = "Quartz Dashboard",
        Endpoint = endpoint
    })
    .WithUrlForEndpoint("http", resource =>
    {
        resource.Url = "/health";
        resource.DisplayText = "Health Checks";
    })
    .WithUrlForEndpoint("https", resource => resource.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WithEnvironment("JustGo__ApiKey", apiKey)
    .WithReference(db)
    .WaitFor(db);

await builder.Build().RunAsync();
