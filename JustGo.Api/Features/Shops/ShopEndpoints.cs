using JustGo.Integrations.JustGo.Features.Shops.Models;

namespace JustGo.Api.Features.Shops;

public static class ShopEndpoints
{
    public static IEndpointRouteBuilder MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shops").WithTags("Shops");

        group.MapGet("/search", async (
            [AsParameters] FindShopsRequest request,
            IShopClient client,
            CancellationToken ct) =>
        {
            var result = await client.SearchShopsAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("SearchShops")
        .WithSummary("Search shops by attributes");

        group.MapGet("/{shopId:guid}", async (Guid shopId, IShopClient client, CancellationToken ct) =>
        {
            var result = await client.GetShopAsync(shopId, ct);
            return Results.Ok(result);
        })
        .WithName("GetShop")
        .WithSummary("Get a shop by ID");

        group.MapGet("/{shopId:guid}/items", async (Guid shopId, IShopClient client, CancellationToken ct) =>
        {
            var result = await client.GetShopItemsAsync(shopId, ct);
            return Results.Ok(result);
        })
        .WithName("GetShopItems")
        .WithSummary("Get items in a shop");

        group.MapGet("/orders/search", async (
            [AsParameters] FindShopOrdersRequest request,
            IShopClient client,
            CancellationToken ct) =>
        {
            var result = await client.SearchOrdersAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("SearchShopOrders")
        .WithSummary("Search shop orders");

        group.MapGet("/orders/{orderId:guid}", async (Guid orderId, IShopClient client, CancellationToken ct) =>
        {
            var result = await client.GetOrderAsync(orderId, ct);
            return Results.Ok(result);
        })
        .WithName("GetShopOrder")
        .WithSummary("Get a shop order by ID");

        return app;
    }
}
