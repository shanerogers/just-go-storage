using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Api.Features.Events;

public static class EventStageEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapEventStageEndpoints()
    {
        var group = app.MapGroup("/events").WithTags("Event Stages");

        group.MapGet("/{eventId:guid}/stages", async (Guid eventId, int pageNumber, int pageSize, IEventClient client, CancellationToken ct) =>
        {
            var result = await client.GetEventStagesAsync(eventId, pageNumber, pageSize, ct);
            return Results.Ok(result);
        })
        .WithName("GetEventStages")
        .WithSummary("Get stages for an event");

        group.MapPost("/{eventId:guid}/stages", async (Guid eventId, EventStageCreateRequest request, IEventClient client, CancellationToken ct) =>
        {
            var result = await client.CreateEventStageAsync(eventId, request, ct);
            return Results.Created($"/events/{eventId}/stages/{result.StageId}", result);
        })
        .WithName("CreateEventStage")
        .WithSummary("Create a stage for an event");

        group.MapDelete("/{eventId:guid}/stages/{stageId:guid}", async (Guid eventId, Guid stageId, IEventClient client, CancellationToken ct) =>
        {
            await client.DeleteEventStageAsync(eventId, stageId, ct);
            return Results.NoContent();
        })
        .WithName("DeleteEventStage")
        .WithSummary("Delete a stage from an event");

        return app;
    }
    }
}
