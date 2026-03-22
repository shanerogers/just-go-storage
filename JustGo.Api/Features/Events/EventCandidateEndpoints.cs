using JustGo.Integrations.JustGo.Features.Events.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Events;

public static class EventCandidateEndpoints
{
    public static IEndpointRouteBuilder MapEventCandidateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Event Candidates");

        group.MapGet("/candidates/search", async (
            [AsParameters] FindEventCandidatesRequest request,
            IJustGoClient client,
            CancellationToken ct) =>
        {
            var result = await client.FindEventCandidatesByAttributesAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("FindEventCandidates")
        .WithSummary("Search event candidates/bookings by attributes");

        group.MapPost("/{eventId:guid}/candidates", async (Guid eventId, EventCandidateCreateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.AddEventCandidateAsync(eventId, request, ct);
            return Results.Created($"/events/{eventId}/candidates/{result.BookingId}", result);
        })
        .WithName("AddEventCandidate")
        .WithSummary("Register a candidate for an event");

        group.MapPut("/candidates/{bookingId:guid}/status", async (Guid bookingId, EventCandidateStatusUpdateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            await client.UpdateEventCandidateStatusAsync(bookingId, request, ct);
            return Results.NoContent();
        })
        .WithName("UpdateEventCandidateStatus")
        .WithSummary("Update the booking status of an event candidate");

        return app;
    }
}
