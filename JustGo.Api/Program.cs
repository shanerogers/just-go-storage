using JustGo.Api;
using JustGo.Api.Features.Auth;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Competitions;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Health;
using JustGo.Api.Services.Jobs;
using JustGo.Integrations.JustGo.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IJustGoClient, StubJustGoClient>();
builder.Services.AddHealthChecks().AddCheck<JustGoHealthCheck>("justgo", tags: ["live"]);
builder.Services.AddQuartz(options =>
{
    var heartbeatJobKey = new JobKey("api-heartbeat");
    options.AddJob<ApiHeartbeatJob>(job => job.WithIdentity(heartbeatJobKey));
    options.AddTrigger(trigger =>
        trigger
            .ForJob(heartbeatJobKey)
            .WithIdentity("api-heartbeat-trigger")
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(5)).RepeatForever()));
});

builder.Services.AddQuartzDashboard();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
// builder.Services.Configure<QuartzOptions>(options =>
// {
//     options["quartz.plugin.jobHistory.type"] = "Quartz.Plugin.History.LoggingJobHistoryPlugin, Quartz.Plugins";
//     options["quartz.plugin.triggerHistory.type"] = "Quartz.Plugin.History.LoggingTriggerHistoryPlugin, Quartz.Plugins";
// });

var application = builder.Build();

application.UseHttpsRedirection();
application.UseExceptionHandler();
application.UseStaticFiles();
application.UseAntiforgery();
application.MapHealthChecks("/health");
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
