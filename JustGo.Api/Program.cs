using JustGo.Api.Data;
using JustGo.Api.Features.Auth;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Competitions;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Health;
using JustGo.Api.Services.Jobs;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;
using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quartz;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.AddNpgsqlDbContext<ApiDbContext>("itkd");

builder.Services.AddHttpLogging(options => options.CombineLogs = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddTransient(_ => TimeProvider.System);
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
    .WithDefaultEntryOptions(options =>
    {
        options.AllowBackgroundDistributedCacheOperations = true;
        options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(2);
        options.DistributedCacheSoftTimeout = TimeSpan.FromMilliseconds(250);
    })
    .TryWithAutoSetup();

builder.Services
    .AddOptions<JustGoOptions>()
    .BindConfiguration(JustGoOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient("JustGoAuth", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<JustGoOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
});

builder.Services
    .AddTransient<JustGoAuthHandler>()
    .AddTransient<IJustGoTokenService, JustGoTokenService>()
    .AddHttpClient<IJustGoClient, JustGoClient>((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<JustGoOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
    })
    .AddHttpMessageHandler<JustGoAuthHandler>();

builder.Services
    .AddHealthChecks()
    .AddCheck<QuartzHealthCheck>("quartz", tags: ["ready"])
    .AddCheck<JustGoHealthCheck>("justgo-api", tags: ["ready"])
    .AddNpgSql(builder.Configuration.GetConnectionString("itkd")!, tags: ["ready"]);

builder.Services
    .AddHealthChecksUI(options => options.AddHealthCheckEndpoint("justgo-api", "/health"))
    .AddPostgreSqlStorage(
        builder.Configuration.GetConnectionString("itkd")!,
        dbOptions => dbOptions.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddQuartz(options =>
{
    options.UsePersistentStore(store =>
    {
        store.UseSystemTextJsonSerializer();
        store.UsePostgres(builder.Configuration.GetConnectionString("itkd")!);
    });

    var syncMembersJobKey = new JobKey("sync-members");
    options.AddJob<SyncMembersJob>(job => job.WithIdentity(syncMembersJobKey).StoreDurably());
    options.AddTrigger(trigger => trigger
        .ForJob(syncMembersJobKey)
        .WithIdentity("sync-members-trigger")
        .WithSimpleSchedule(schedule => schedule
            .WithInterval(TimeSpan.FromHours(1))
            .RepeatForever()));
});

builder.Services.AddQuartzDashboard();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

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
application.MapQuartzDashboard();
application.MapHealthChecks("/health", new() { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
application.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

application
    .MapAuthEndpoints()
    .MapClubEndpoints()
    .MapCompetitionEndpoints()
    .MapCredentialEndpoints()
    .MapEventEndpoints()
    .MapEventCandidateEndpoints()
    .MapEventPromoterEndpoints()
    .MapEventStageEndpoints();

await application.RunAsync();
