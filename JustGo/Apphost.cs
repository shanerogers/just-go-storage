using JustGo;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DashboardApplicationName = "Just Go"
});

var database = builder.AddItkdDatabase();
var cache = builder.AddAppCache();
var apiKey = builder.AddParameter("justgo-apikey", secret: true);

var api = builder.AddJustGoApi(database, cache, apiKey);

builder.AddGradingApp(api);

await builder.Build().RunAsync();
