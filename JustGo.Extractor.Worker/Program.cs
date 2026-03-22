using JustGo.Extractor.Worker.Extraction.Configuration;
using JustGo.Extractor.Worker.Extraction.Contracts;
using JustGo.Extractor.Worker.Extraction.Data;
using JustGo.Extractor.Worker.Extraction.Jobs;
using JustGo.Extractor.Worker.Extraction.Services;
using JustGo.Integrations.JustGo.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJustGoIntegration(builder.Configuration);
builder.AddNpgsqlDbContext<ExtractionDbContext>("justgoextractiondb");

builder.Services.Configure<JustGoExtractionOptions>(
    builder.Configuration.GetSection(JustGoExtractionOptions.SectionName));

builder.Services.AddScoped<IDocumentStore, DocumentStore>();
builder.Services.AddScoped<IExtractionJobStateStore, ExtractionJobStateStore>();
builder.Services.AddScoped<IJustGoDomainExtractor, EventsDomainExtractor>();
builder.Services.AddScoped<IJustGoDomainExtractor, ClubsDomainExtractor>();
builder.Services.AddScoped<IJustGoDomainExtractor, CredentialsDomainExtractor>();
builder.Services.AddScoped<IJustGoExtractionOrchestrator, JustGoExtractionOrchestrator>();
builder.Services.AddHostedService<ExtractionStartupService>();

builder.Services.AddQuartz(options =>
{
    options.SchedulerId = "AUTO";
    options.SchedulerName = QuartzJobSettings.SchedulerName;

    options.UsePersistentStore(store =>
    {
        store.PerformSchemaValidation = false;
        store.UseProperties = true;
        store.UseSystemTextJsonSerializer();
        store.UsePostgres(provider =>
        {
            provider.TablePrefix = "QRTZ_";
            provider.ConnectionString = builder.Configuration.GetConnectionString("justgoextractiondb")
                ?? throw new InvalidOperationException("Connection string 'justgoextractiondb' is required.");
        }, "default");
    });

    var extractionJobKey = new JobKey(QuartzJobSettings.ExtractJobName, QuartzJobSettings.ExtractJobGroup);
    options.AddJob<RunDailyExtractionJob>(job => job.WithIdentity(extractionJobKey));
    options.AddTrigger(trigger =>
    {
        var extraction = builder.Configuration
            .GetSection(JustGoExtractionOptions.SectionName)
            .Get<JustGoExtractionOptions>() ?? new JustGoExtractionOptions();

        trigger
            .WithIdentity(QuartzJobSettings.TriggerName, QuartzJobSettings.TriggerGroup)
            .ForJob(extractionJobKey)
            .WithCronSchedule(extraction.DailyCron, cron =>
            {
                cron.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(extraction.TimeZoneId));
            });
    });
});

builder.Services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
builder.Services.AddQuartzDashboard(options =>
{
    options.ApiPath = "/quartz-api";
    options.DashboardPath = "/quartz";
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect("/quartz")).AllowAnonymous();
app.MapHealthChecks("/health");
app.MapQuartzDashboard();

app.Run();
