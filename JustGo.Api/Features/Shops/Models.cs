namespace JustGo.Integrations.JustGo.Features.Shops.Models;

public sealed class FindShopsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
}

public sealed class FindShopOrdersRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? MemberId { get; set; }
}
