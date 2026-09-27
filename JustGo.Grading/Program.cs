using JustGo.Grading.Components;
using Microsoft.AspNetCore.HttpLogging;
using MudBlazor.Services;
using Humanizer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpLogging(options =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
    options.ResponseBodyLogLimit = (int)10.Megabytes().Bytes;
});

builder.Services.AddMudServices();

builder.Services
    .AddOptions<JustGo.Grading.GradingOptions>()
    .BindConfiguration(JustGo.Grading.GradingOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Replace the 10s service-default timeouts for this client only; JustGo calls behind our API are slow.
#pragma warning disable EXTEXP0001
builder.Services.AddHttpClient("JustGoApi", client =>
{
    client.BaseAddress = new Uri("https+http://api");
})
.AddServiceDiscovery()
.RemoveAllResilienceHandlers()
.AddStandardResilienceHandler(JustGo.Grading.JustGoApiResilience.Configure);
#pragma warning restore EXTEXP0001

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapPost("/telemetry/browser-error", async (HttpContext ctx, ILoggerFactory loggerFactory) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    var logger = loggerFactory.CreateLogger("Browser");
    logger.LogError("Browser error: {ErrorJson}", body);
    return Results.NoContent();
}).ExcludeFromDescription();

app.Run();
