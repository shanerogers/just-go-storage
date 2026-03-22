using JustGo.Integrations.JustGo.Features.Events.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Events;

public static class EventStageEndpoints
{
    public static IEndpointRouteBuilder MapEventStageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Event Stages");

        group.MapGet("/{eventId:guid}/stages", async (Guid eventId, int pageNumber, int pageSize, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetEventStagesAsync(eventId, pageNumber, pageSize, ct);
            return Results.Ok(result);
        })
        .WithName("GetEventStages")
        .WithSummary("Get stages for an event");

        group.MapPost("/{eventId:guid}/stages", async (Guid eventId, EventStageCreateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.CreateEventStageAsync(eventId, request, ct);
            return Results.Created($"/events/{eventId}/stages/{result.StageId}", result);
        })
        .WithName("CreateEventStage")
        .WithSummary("Create a stage for an event");

        return app;
    }
}
