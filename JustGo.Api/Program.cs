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
using JustGo.Api;

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

builder.Services.AddTickerQ(options =>
{
    options.AddDashboard();
    options.AddOpenTelemetryInstrumentation();
    options.AddOperationalStore(options =>
        options.UseApplicationDbContext<ApiDbContext>(ConfigurationType.UseModelCustomizer));
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
