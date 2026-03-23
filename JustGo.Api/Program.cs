using JustGo.Api;
using JustGo.Api.Features.Auth;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Competitions;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Health;
using JustGo.Integrations.JustGo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddJustGoIntegration(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<JustGoHealthCheck>("justgo", tags: ["live"]);

var application = builder.Build();

application.UseHttpsRedirection();
application.UseExceptionHandler();
application.MapHealthChecks("/health");
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
