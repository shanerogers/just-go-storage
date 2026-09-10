using JustGo.Integrations.JustGo.Features.Shops.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Shops;

public sealed class ShopClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IShopClient
{
    public Task<object> SearchShopsAsync(FindShopsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };
        if (request.Name is not null)
        {
            query["Name"] = request.Name;
        }

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Shops/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<object> GetShopAsync(Guid shopId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Shops/{shopId}", ct);

    public Task<object> GetShopItemsAsync(Guid shopId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Shops/{shopId}/Items", ct);

    public Task<object> SearchOrdersAsync(FindShopOrdersRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };
        if (request.MemberId is not null)
        {
            query["memberId"] = request.MemberId.Value.ToString();
        }

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Shops/Orders/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<object> GetOrderAsync(Guid orderId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Shops/Orders/{orderId}", ct);
}
