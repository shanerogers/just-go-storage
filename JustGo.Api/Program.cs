using JustGo.Api.Data;
using JustGo.Api.Features.Auth;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Cache;
using JustGo.Api.Features.Competitions;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Members;
using JustGo.Api.Features.Memberships;
using JustGo.Api.Features.Organisations;
using JustGo.Api.Features.Rewards;
using JustGo.Api.Features.Shops;
using JustGo.Api.Features.JustGoUpstream;
using JustGo.Api.Health;
using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TickerQ.DependencyInjection;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Instrumentation.OpenTelemetry;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using Scalar.AspNetCore;
using JustGo.Api;
using JustGo.Integrations.JustGo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");
builder.AddNpgsqlDbContext<ApiDbContext>("itkd", configureDbContextOptions: options => options
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .ConfigureWarnings(warningsHandler => warningsHandler.Throw())
    .UseNpgsql(npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddHttpLogging(options => options.CombineLogs = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddAntiforgery();
builder.Services.AddTransient(_ => TimeProvider.System);
builder.Services.AddOpenApi();

builder.Services.AddTickerQ(options =>
{
    options.AddDashboard();
    options.AddOpenTelemetryInstrumentation();
    options.AddOperationalStore(ef => ef.UseApplicationDbContext<ApiDbContext>(ConfigurationType.UseModelCustomizer));
});

builder.Services.MapTicker<SyncMembersJob>()
    .WithCron(Cronos.CronExpression.Hourly.ToString())
    .WithMaxConcurrency(1);

builder.Services.AddSingleton<IFusionCacheSerializer, FusionCacheSystemTextJsonSerializer>();
builder.Services.AddSingleton<IFusionCacheBackplane>(sp =>
{
    var redisConnectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("cache")!;
    return new RedisBackplane(new RedisBackplaneOptions
    {
        Configuration = redisConnectionString
    });
});

builder.Services
    .AddFusionCache()
    .TryWithAutoSetup();

builder.Services.AddJustGoClient();

builder.Services
    .AddHealthChecks()
    .AddCheck<TickerQHealthCheck>("tickerq", tags: ["ready"])
    .AddCheck<JustGoHealthCheck>("justgo-api", tags: ["ready"])
    .AddNpgSql(builder.Configuration.GetConnectionString("itkd")!, tags: ["ready"]);

builder.Services
    .AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(60_000);
        options.AddHealthCheckEndpoint("justgo-api", "/health");
    })
    .AddPostgreSqlStorage(
        builder.Configuration.GetConnectionString("itkd")!,
        dbOptions => dbOptions.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));


var application = builder.Build();

await using (var scope = application.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ApiDbContext>()
        .Database
        .MigrateAsync();
}

application.UseHttpLogging();
application.UseHttpsRedirection();
application.UseExceptionHandler();
application.UseStaticFiles();
application.UseAntiforgery();
application.MapHealthChecks("/health", new() { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
application.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

application.UseTickerQ();

// Development-only: inject API key into Scalar requests
if (application.Environment.IsDevelopment())
{
    var apiKey = builder.Configuration["JustGo:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        application.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/scalar"))
            {
                // Scalar UI can use this via X-Api-Key header for direct requests
                context.Response.Headers["X-Default-Api-Key"] = apiKey;
            }
            await next();
        });
    }
}

application.MapOpenApi();
application.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/openapi/v1.json");
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Development-only: second Scalar UI showing JustGo's own upstream spec, proxied
// same-origin through this API (with its bearer token) to avoid browser CORS.
if (application.Environment.IsDevelopment())
{
    application.MapJustGoUpstreamEndpoints();
    application.MapScalarApiReference("/scalar/justgo-upstream", async (options, context) =>
    {
        options.WithTitle("JustGo API (Upstream Sandbox)");
        options.WithOpenApiRoutePattern("/openapi/justgo-upstream.json");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

        // Remember whatever auth value is in Scalar's UI across page reloads, and pre-fill it
        // with a live token on every load so it's usually a no-op. JustGo's upstream spec
        // declares its "Bearer" scheme as an apiKey-style header (not http-bearer), so Scalar
        // sends this value verbatim as the Authorization header -- it needs the "Bearer "
        // prefix included. Note the reverse proxy above already re-authenticates every
        // forwarded request server-side regardless of what's sent here, so this is purely for
        // convenience (e.g. copying a working C# snippet that already has a valid token).
        options.EnablePersistentAuthentication();
        var tokenService = context.RequestServices.GetRequiredService<IJustGoTokenService>();
        var token = await tokenService.GetTokenAsync(context.RequestAborted);
        options.AddApiKeyAuthentication("Bearer", scheme => scheme.Value = $"Bearer {token}");
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

application
    .MapAuthEndpoints()
    .MapClubEndpoints()
    .MapCompetitionEndpoints()
    .MapCredentialEndpoints()
    .MapEventEndpoints()
    .MapEventCandidateEndpoints()
    .MapEventPromoterEndpoints()
    .MapEventStageEndpoints()
    .MapMemberEndpoints()
    .MapMembershipEndpoints()
    .MapOrganisationEndpoints()
    .MapShopEndpoints()
    .MapRewardEndpoints();

if (application.Environment.IsDevelopment())
{
    application.MapCacheAdminEndpoints();
}

await application.RunAsync();




