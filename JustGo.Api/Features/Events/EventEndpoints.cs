using JustGo.Integrations.JustGo.Features.Events.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Events;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapGet("/search", async (
            [AsParameters] FindEventsRequest request,
            IJustGoClient client,
            CancellationToken ct) =>
        {
            var result = await client.FindEventsByAttributesAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("FindEvents")
        .WithSummary("Search events by attributes");

        group.MapGet("/{eventId:guid}", async (Guid eventId, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetEventAsync(eventId, ct);
            return Results.Ok(result);
        })
        .WithName("GetEvent")
        .WithSummary("Get an event by ID");

        group.MapPost("/", async (EventCreateRequest request, Guid? templateId, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.CreateEventAsync(request, templateId, ct);
            return Results.Created($"/events/{result.EventId}", result);
        })
        .WithName("CreateEvent")
        .WithSummary("Create a new event");

        group.MapPut("/{eventId:guid}", async (Guid eventId, EventUpdateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            await client.UpdateEventAsync(eventId, request, ct);
            return Results.NoContent();
        })
        .WithName("UpdateEvent")
        .WithSummary("Update an event");

        group.MapGet("/{eventId:guid}/tickets", async (Guid eventId, int pageNumber, int pageSize, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetEventTicketsAsync(eventId, pageNumber, pageSize, ct);
            return Results.Ok(result);
        })
        .WithName("GetEventTickets")
        .WithSummary("Get tickets for an event");

        return app;
    }
}
