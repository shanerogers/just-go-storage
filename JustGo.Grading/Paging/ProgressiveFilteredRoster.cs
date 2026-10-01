namespace JustGo.Grading.Paging;

/// <summary>
/// Scans a paged source on demand, publishing matching rows as each detail lookup completes.
/// The final slot is a loading marker until the source has been exhausted.
/// </summary>
public sealed class ProgressiveFilteredRoster<TSource, TItem> : IDisposable where TItem : class
{
    private const int DetailConcurrency = 3;
    private readonly int _pageSize;
    private readonly Func<int, CancellationToken, Task<PageResult<TSource>>> _fetchPage;
    private readonly Func<TSource, CancellationToken, Task<TItem?>> _resolve;
    private readonly IComparer<TItem> _order;
    private readonly TItem _loadingMarker;
    private readonly bool _scanToEnd;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _gate = new();
    private readonly List<TItem> _items = [];
    private Task? _scan;
    private int _nextPage = 1;
    private int _requestedCount;
    private bool _complete;
    private bool _disposed;

    public ProgressiveFilteredRoster(
        int pageSize,
        Func<int, CancellationToken, Task<PageResult<TSource>>> fetchPage,
        Func<TSource, CancellationToken, Task<TItem?>> resolve,
        TItem loadingMarker,
        IComparer<TItem> order,
        bool scanToEnd = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        _pageSize = pageSize;
        _fetchPage = fetchPage;
        _resolve = resolve;
        _loadingMarker = loadingMarker;
        _order = order;
        _scanToEnd = scanToEnd;
    }

    public event Action? Changed;

    public IReadOnlyList<TItem> LoadedItems
    {
        get
        {
            lock (_gate)
            {
                return _items.ToList();
            }
        }
    }

    public int? TotalCount
    {
        get
        {
            lock (_gate)
            {
                return _complete || _items.Count > 0 ? _items.Count + (_complete ? 0 : 1) : null;
            }
        }
    }

    public bool IsComplete
    {
        get
        {
            lock (_gate)
            {
                return _complete;
            }
        }
    }

    public Exception? Error { get; private set; }

    public PageResult<TItem> GetRange(int startIndex, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        lock (_gate)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProgressiveFilteredRoster<TSource, TItem>));
            }

            if (count > 0 && !_complete && Error is null)
            {
                _requestedCount = Math.Max(_requestedCount, startIndex + count);
                if (_scan is null && (_scanToEnd || _items.Count < _requestedCount))
                {
                    _scan = Task.Run(ScanAsync);
                }
            }

            var total = _items.Count + (_complete || Error is not null ? 0 : 1);
            var available = _items.Skip(startIndex).Take(count).ToList();
            if (count > available.Count && startIndex <= _items.Count
                && startIndex + count > _items.Count && !_complete && Error is null)
            {
                available.Add(_loadingMarker);
            }

            return new PageResult<TItem>(available, total);
        }
    }

    private async Task ScanAsync()
    {
        try
        {
            while (NeedsMore())
            {
                var page = await _fetchPage(_nextPage++, _stop.Token);
                using var source = page.Items.GetEnumerator();
                var pending = new List<Task<TItem?>>();
                while (pending.Count < DetailConcurrency && source.MoveNext())
                {
                    pending.Add(_resolve(source.Current, _stop.Token));
                }

                while (pending.Count > 0)
                {
                    var finished = await Task.WhenAny(pending);
                    pending.Remove(finished);
                    var resolved = await finished;
                    if (resolved is not null)
                    {
                        lock (_gate)
                        {
                            var index = _items.BinarySearch(resolved, _order);
                            _items.Insert(index < 0 ? ~index : index, resolved);
                        }
                        Changed?.Invoke();
                    }

                    if (source.MoveNext())
                    {
                        pending.Add(_resolve(source.Current, _stop.Token));
                    }
                }

                var scanned = (_nextPage - 2) * _pageSize + page.Items.Count;
                lock (_gate)
                {
                    _complete = page.Items.Count < _pageSize || scanned >= page.TotalCount;
                }
                Changed?.Invoke();
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            lock (_gate)
            {
                Error = ex;
            }
            Changed?.Invoke();
        }
        finally
        {
            lock (_gate)
            {
                _scan = null;
                if (!_disposed && !_complete && Error is null && (_scanToEnd || _items.Count < _requestedCount))
                {
                    _scan = Task.Run(ScanAsync);
                }
            }
        }
    }

    private bool NeedsMore()
    {
        lock (_gate)
        {
            return !_complete && !_disposed && (_scanToEnd || _items.Count < _requestedCount);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stop.Cancel();
        }
    }
}
