
var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("justgo-apikey", secret: true);
var postgres = builder.AddPostgres("justgopostgres");
var extractionDb = postgres.AddDatabase("justgoextractiondb");

builder.AddProject<Projects.JustGo_Api>("api")
    .WithEnvironment("JustGo__ApiKey", apiKey)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.JustGo_Extractor_Worker>("extractor")
    .WithEnvironment("JustGo__ApiKey", apiKey)
    .WithReference(extractionDb)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
