using JustGo.Integrations.JustGo.Features.Events.Models;
using JustGo.Integrations.JustGo.Services;

namespace JustGo.Api.Features.Events;

public static class EventPromoterEndpoints
{
    public static IEndpointRouteBuilder MapEventPromoterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Event Promoters");

        group.MapGet("/{eventId:guid}/promoters", async (Guid eventId, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.GetEventPromotersAsync(eventId, ct);
            return Results.Ok(result);
        })
        .WithName("GetEventPromoters")
        .WithSummary("Get all promoters for an event");

        group.MapPost("/{eventId:guid}/promoters", async (Guid eventId, EventPromoterCreateRequest request, IJustGoClient client, CancellationToken ct) =>
        {
            var result = await client.AddEventPromoterAsync(eventId, request, ct);
            return Results.Created($"/events/{eventId}/promoters/{result.PromoterId}", result);
        })
        .WithName("AddEventPromoter")
        .WithSummary("Add a promoter to an event");

        group.MapDelete("/{eventId:guid}/promoters/{promoterId:int}", async (Guid eventId, int promoterId, IJustGoClient client, CancellationToken ct) =>
        {
            await client.RemoveEventPromoterAsync(eventId, promoterId, ct);
            return Results.NoContent();
        })
        .WithName("RemoveEventPromoter")
        .WithSummary("Remove a promoter from an event");

        return app;
    }
}
