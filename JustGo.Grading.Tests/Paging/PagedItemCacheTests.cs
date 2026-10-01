using JustGo.Grading.Paging;

namespace JustGo.Grading.Tests.Paging;

public sealed class PagedItemCacheTests
{
    private const int PageSize = 10;

    [Fact]
    public async Task GetRangeAsync_WithinOnePage_FetchesOnlyThatPage()
    {
        var source = new FakePagedSource(totalItems: 35);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);

        var result = await cache.GetRangeAsync(12, 5, CancellationToken.None);

        Assert.Equal([12, 13, 14, 15, 16], result.Items);
        Assert.Equal(35, result.TotalCount);
        Assert.Equal([2], source.RequestedPages);
    }

    [Fact]
    public async Task GetRangeAsync_SpanningPages_StitchesItemsAcrossPages()
    {
        var source = new FakePagedSource(totalItems: 35);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);

        var result = await cache.GetRangeAsync(8, 14, CancellationToken.None);

        Assert.Equal(Enumerable.Range(8, 14), result.Items);
        Assert.Equal([1, 2, 3], source.RequestedPages);
    }

    [Fact]
    public async Task GetRangeAsync_WhenScrollingBack_ReusesLoadedPagesAndItems()
    {
        var source = new FakePagedSource(totalItems: 35);
        var cache = new PagedItemCache<Box>(PageSize, source.FetchBoxesAsync);

        var first = await cache.GetRangeAsync(0, 5, CancellationToken.None);
        first.Items[0].IsSelected = true;
        await cache.GetRangeAsync(20, 5, CancellationToken.None);
        var again = await cache.GetRangeAsync(0, 5, CancellationToken.None);

        Assert.True(again.Items[0].IsSelected);
        Assert.Equal([1, 3], source.RequestedPages);
    }

    [Fact]
    public async Task GetRangeAsync_PastTheEnd_ReturnsOnlyRemainingItems()
    {
        var source = new FakePagedSource(totalItems: 23);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);

        var result = await cache.GetRangeAsync(18, 10, CancellationToken.None);

        Assert.Equal([18, 19, 20, 21, 22], result.Items);
        Assert.Equal(23, result.TotalCount);
    }

    [Fact]
    public async Task GetRangeAsync_WhenSourceOverReportsTotal_CorrectsTotalFromShortPage()
    {
        var source = new FakePagedSource(totalItems: 13, reportedTotal: 500);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);

        await cache.GetRangeAsync(0, 10, CancellationToken.None);
        Assert.Equal(500, cache.TotalCount);

        var result = await cache.GetRangeAsync(10, 10, CancellationToken.None);

        Assert.Equal(13, result.TotalCount);
        Assert.Equal([10, 11, 12], result.Items);
    }

    [Fact]
    public async Task GetRangeAsync_RaisesPageLoadedOncePerFetchedPage()
    {
        var source = new FakePagedSource(totalItems: 35);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);
        var loadedPageSizes = new List<int>();
        cache.PageLoaded += items => loadedPageSizes.Add(items.Count);

        await cache.GetRangeAsync(0, 15, CancellationToken.None);
        await cache.GetRangeAsync(5, 10, CancellationToken.None);

        Assert.Equal([10, 10], loadedPageSizes);
        Assert.Equal(20, cache.LoadedItems.Count());
    }

    [Fact]
    public async Task GetRangeAsync_ScrollingDown_FetchesTheNextPageOnlyWhenReached()
    {
        var source = new FakePagedSource(totalItems: 35);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);

        await cache.GetRangeAsync(0, 9, CancellationToken.None);
        await cache.GetRangeAsync(2, 8, CancellationToken.None);
        Assert.Equal([1], source.RequestedPages);

        await cache.GetRangeAsync(5, 9, CancellationToken.None);
        Assert.Equal([1, 2], source.RequestedPages);
    }

    [Fact]
    public async Task GetRangeAsync_WithEmptySource_ReturnsNoItems()
    {
        var source = new FakePagedSource(totalItems: 0);
        var cache = new PagedItemCache<int>(PageSize, source.FetchAsync);

        var result = await cache.GetRangeAsync(0, 10, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    private sealed class Box(int value)
    {
        public int Value { get; } = value;
        public bool IsSelected { get; set; }
    }

    private sealed class FakePagedSource(int totalItems, int? reportedTotal = null)
    {
        public List<int> RequestedPages { get; } = [];

        public Task<PageResult<int>> FetchAsync(int pageNumber, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            RequestedPages.Add(pageNumber);
            var items = Enumerable.Range((pageNumber - 1) * PageSize, PageSize)
                .Where(index => index < totalItems)
                .ToList();
            return Task.FromResult(new PageResult<int>(items, reportedTotal ?? totalItems));
        }

        public async Task<PageResult<Box>> FetchBoxesAsync(int pageNumber, CancellationToken ct)
        {
            var page = await FetchAsync(pageNumber, ct);
            return new PageResult<Box>(page.Items.Select(value => new Box(value)).ToList(), page.TotalCount);
        }
    }
}
