namespace JustGo.Grading.Paging;

/// <summary>One page of items plus the total the source reported across all pages.</summary>
public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>
/// Serves arbitrary item ranges (as requested by an infinite-scroll/virtualized list) from a
/// 1-based paged source, fetching each page at most once and keeping the loaded items so state
/// on them (e.g. selection) survives scrolling away and back.
/// </summary>
public sealed class PagedItemCache<T>
{
    private readonly int _pageSize;
    private readonly Func<int, CancellationToken, Task<PageResult<T>>> _fetchPage;
    private readonly Dictionary<int, IReadOnlyList<T>> _pages = [];
    private int? _totalCount;

    public PagedItemCache(int pageSize, Func<int, CancellationToken, Task<PageResult<T>>> fetchPage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        _pageSize = pageSize;
        _fetchPage = fetchPage;
    }

    /// <summary>Raised after a page is fetched from the source for the first time.</summary>
    public event Action<IReadOnlyList<T>>? PageLoaded;

    public int? TotalCount => _totalCount;

    public IEnumerable<T> LoadedItems => _pages.OrderBy(page => page.Key).SelectMany(page => page.Value);

    public async Task<PageResult<T>> GetRangeAsync(int startIndex, int count, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        if (count <= 0 || (_totalCount is { } known && startIndex >= known))
        {
            return new PageResult<T>([], _totalCount ?? 0);
        }

        var firstPage = (startIndex / _pageSize) + 1;
        var lastPage = ((startIndex + count - 1) / _pageSize) + 1;
        var items = new List<T>();

        for (var pageNumber = firstPage; pageNumber <= lastPage; pageNumber++)
        {
            var page = await GetPageAsync(pageNumber, ct);
            items.AddRange(page);

            if (page.Count < _pageSize)
            {
                break;
            }
        }

        var offset = startIndex - ((firstPage - 1) * _pageSize);
        return new PageResult<T>(items.Skip(offset).Take(count).ToList(), _totalCount ?? 0);
    }

    private async Task<IReadOnlyList<T>> GetPageAsync(int pageNumber, CancellationToken ct)
    {
        if (_pages.TryGetValue(pageNumber, out var cached))
        {
            return cached;
        }

        var result = await _fetchPage(pageNumber, ct);
        _pages[pageNumber] = result.Items;
        _totalCount = ResolveTotalCount(pageNumber, result);
        PageLoaded?.Invoke(result.Items);
        return result.Items;
    }

    // A short page marks the true end of the list, which corrects totals the source over-reports
    // (for example after local filtering); a full page means at least this many items exist.
    private int ResolveTotalCount(int pageNumber, PageResult<T> result)
    {
        var itemsThroughPage = ((pageNumber - 1) * _pageSize) + result.Items.Count;
        return result.Items.Count < _pageSize
            ? itemsThroughPage
            : Math.Max(result.TotalCount, itemsThroughPage);
    }
}
