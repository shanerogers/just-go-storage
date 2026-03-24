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

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ApiDbContext>("itkd");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(_ => { });

builder.Services
    .AddOptions<JustGoOptions>()
    .BindConfiguration(JustGoOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<JustGoTokenService>();
builder.Services.AddTransient<JustGoAuthHandler>();
builder.Services
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

    var heartbeatJobKey = new JobKey("api-heartbeat");
    options.AddJob<ApiHeartbeatJob>(job => job.WithIdentity(heartbeatJobKey).StoreDurably());
    options.AddTrigger(trigger => trigger
        .ForJob(heartbeatJobKey)
        .WithIdentity("api-heartbeat-trigger")
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(5)).RepeatForever()));

    var syncMembersJobKey = new JobKey("sync-members");
    options.AddJob<SyncMembersJob>(job => job.WithIdentity(syncMembersJobKey).StoreDurably());
    options.AddTrigger(trigger => trigger
        .ForJob(syncMembersJobKey)
        .WithIdentity("sync-members-trigger")
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromHours(1)).RepeatForever()));
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
