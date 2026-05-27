using JustGo.Integrations.JustGo.Features.Shops.Models;

namespace JustGo.Api.Features.Shops;

public interface IShopClient
{
    Task<object> SearchShopsAsync(FindShopsRequest request, CancellationToken ct);
    Task<object> GetShopAsync(Guid shopId, CancellationToken ct);
    Task<object> GetShopItemsAsync(Guid shopId, CancellationToken ct);
    Task<object> SearchOrdersAsync(FindShopOrdersRequest request, CancellationToken ct);
    Task<object> GetOrderAsync(Guid orderId, CancellationToken ct);
}
