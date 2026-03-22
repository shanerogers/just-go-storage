var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("justgo-apikey", secret: true);

var db = builder
    .AddPostgres("just-go")
    .WithDataVolume();

var extractionDb = db.AddDatabase("tkd-nz");

extractionDb.WithChildRelationship(db);

builder.AddProject<Projects.JustGo_Api>("api")
    .WithEnvironment("JustGo__ApiKey", apiKey)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.JustGo_Extractor_Worker>("extractor")
    .WithEnvironment("JustGo__ApiKey", apiKey)
    .WithReference(extractionDb)
    .WaitFor(extractionDb)
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();
