using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Api.Features.Events;

public static class EventEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapEventEndpoints()
        {
            var group = app.MapGroup("/events").WithTags("Events");

            group.MapGet("/search", async (
                [AsParameters] FindEventsRequest request,
                IEventClient client,
                CancellationToken ct) =>
            {
                var result = await client.FindEventsByAttributesAsync(request, ct);
                return Results.Ok(result);
            })
            .WithName("FindEvents")
            .WithSummary("Search events by attributes");

            group.MapGet("/{eventId:guid}", async (Guid eventId, IEventClient client, CancellationToken ct) =>
            {
                var result = await client.GetEventAsync(eventId, ct);
                return Results.Ok(result);
            })
            .WithName("GetEvent")
            .WithSummary("Get an event by ID");

            group.MapPost("/", async (EventCreateRequest request, Guid? templateId, IEventClient client, CancellationToken ct) =>
            {
                var result = await client.CreateEventAsync(request, templateId, ct);
                return Results.Created($"/events/{result.EventId}", result);
            })
            .WithName("CreateEvent")
            .WithSummary("Create a new event");

            group.MapPut("/{eventId:guid}", async (Guid eventId, EventUpdateRequest request, IEventClient client, CancellationToken ct) =>
            {
                await client.UpdateEventAsync(eventId, request, ct);
                return Results.NoContent();
            })
            .WithName("UpdateEvent")
            .WithSummary("Update an event");

            group.MapGet("/{eventId:guid}/tickets", async (Guid eventId, int pageNumber, int pageSize, IEventClient client, CancellationToken ct) =>
            {
                var result = await client.GetEventTicketsAsync(eventId, pageNumber, pageSize, ct);
                return Results.Ok(result);
            })
            .WithName("GetEventTickets")
            .WithSummary("Get tickets for an event");

            group.MapGet("/schema", async (IEventClient client, CancellationToken ct) =>
            {
                var result = await client.GetSchemaAsync(ct);
                return Results.Ok(result);
            })
            .WithName("GetEventSchema")
            .WithSummary("Get the event data schema");

            return app;
        }
    }
}
