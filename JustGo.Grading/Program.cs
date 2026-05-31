using JustGo.Grading.Components;
using Microsoft.AspNetCore.HttpLogging;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpLogging(options =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
});

builder.Services.AddMudServices();

builder.Services.AddHttpClient("JustGoApi", client =>
{
    client.BaseAddress = new Uri("https+http://api");
}).AddServiceDiscovery();

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
