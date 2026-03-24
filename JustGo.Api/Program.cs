using JustGo.Api;
using JustGo.Api.Data;
using JustGo.Api.Features.Auth;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Competitions;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Health;
using JustGo.Api.Services.Jobs;
using JustGo.Integrations.JustGo.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
const string DatabaseConnectionName = "tkd-nz";

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddScoped<IJustGoClient, StubJustGoClient>();
builder.AddNpgsqlDbContext<ApiDbContext>(DatabaseConnectionName);

builder.Services.AddHealthChecks()
    .AddCheck<JustGoHealthCheck>("justgo-api", tags: ["ready"])
    .AddCheck<QuartzHealthCheck>("quartz", tags: ["ready"])
    .AddNpgSql(builder.Configuration.GetConnectionString(DatabaseConnectionName)!, tags: ["ready"]);

var healthUIBuilder = builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30);
    options.SetMinimumSecondsBetweenFailureNotifications(60);
    options.MaximumHistoryEntriesPerEndpoint(60);
    options.AddHealthCheckEndpoint("justgo-api", "/health");
});

healthUIBuilder.AddPostgreSqlStorage(builder.Configuration.GetConnectionString(DatabaseConnectionName)!);

builder.Services.AddQuartz(options =>
{
    options.UsePersistentStore(store =>
    {
        store.UsePostgres(builder.Configuration.GetConnectionString(DatabaseConnectionName)!);
        store.UseSystemTextJsonSerializer();
    });

    var heartbeatJobKey = new JobKey("api-heartbeat");
    options.AddJob<ApiHeartbeatJob>(job => job.WithIdentity(heartbeatJobKey).StoreDurably());
    options.AddTrigger(trigger => trigger
        .ForJob(heartbeatJobKey)
        .WithIdentity("api-heartbeat-trigger")
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(5)).RepeatForever()));
});

builder.Services.AddQuartzDashboard();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var application = builder.Build();

await using (var scope = application.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    await dbContext.Database.MigrateAsync();
}

application.UseHttpsRedirection();
application.UseExceptionHandler();
application.UseStaticFiles();
application.UseAntiforgery();
application.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});
application.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});
application.MapQuartzDashboard();

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
