using JustGo.Grading.Paging;

namespace JustGo.Grading.Tests.Paging;

public sealed class ProgressiveFilteredRosterTests
{
    [Fact]
    public async Task ReturnsEligibleMembersAsDetailsArriveInAlphabeticOrder()
    {
        var bravo = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var alpha = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var changed = new SemaphoreSlim(0);
        var requests = new List<int>();
        using var roster = CreateRoster(
            (page, _) =>
            {
                requests.Add(page);
                return Task.FromResult(new PageResult<string>(["Bravo", "Alpha", "Dan"], 3));
            },
            (name, _) => name switch
            {
                "Bravo" => bravo.Task,
                "Alpha" => alpha.Task,
                _ => Task.FromResult<string?>(null)
            });
        roster.Changed += () => changed.Release();

        Assert.Equal(["Loading"], roster.GetRange(0, 10).Items);

        bravo.SetResult("Bravo");
        await changed.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(["Bravo", "Loading"], roster.GetRange(0, 10).Items);

        alpha.SetResult("Alpha");
        await WaitUntilAsync(() => roster.IsComplete, changed);
        Assert.Equal(["Alpha", "Bravo"], roster.GetRange(0, 10).Items);
        Assert.Equal(2, roster.TotalCount);
        Assert.Equal([1], requests);
    }

    [Fact]
    public async Task ScansPastExcludedPagesUntilItFindsEligibleMembers()
    {
        var changed = new SemaphoreSlim(0);
        var requests = new List<int>();
        using var roster = CreateRoster(
            (page, _) =>
            {
                requests.Add(page);
                return Task.FromResult(new PageResult<string>(
                    page == 1 ? ["Dan", "1st Gup", "Another Dan"] : ["Able"], 4));
            },
            (name, _) => Task.FromResult<string?>(name == "Able" ? name : null));
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 10);
        await WaitUntilAsync(() => roster.IsComplete, changed);

        Assert.Equal(["Able"], roster.GetRange(0, 10).Items);
        Assert.Equal([1, 2], requests);
        Assert.Null(roster.Error);
    }

    [Fact]
    public async Task FailedGradeLookupIsReportedRatherThanTreatingTheRosterAsEmpty()
    {
        var changed = new SemaphoreSlim(0);
        using var roster = CreateRoster(
            (_, _) => Task.FromResult(new PageResult<string>(["Able"], 1)),
            (_, _) => Task.FromException<string?>(new HttpRequestException("Grade lookup failed")));
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 10);
        await WaitUntilAsync(() => roster.Error is not null, changed);

        Assert.IsType<HttpRequestException>(roster.Error);
        Assert.Empty(roster.GetRange(0, 10).Items);
    }

    [Fact]
    public async Task NextPageIsFetchedOnlyWhenScrollingNeedsAnotherEligibleMember()
    {
        var changed = new SemaphoreSlim(0);
        var requested = new List<int>();
        using var roster = CreateRoster(
            (page, _) =>
            {
                requested.Add(page);
                return Task.FromResult(new PageResult<string>(
                    page == 1 ? ["Bravo", "Charlie", "Dan"] : ["Alpha"], 4));
            },
            (name, _) => Task.FromResult<string?>(name == "Dan" ? null : name));
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 2);
        await WaitUntilAsync(() => roster.LoadedItems.Count == 2, changed);
        Assert.Equal([1], requested);
        Assert.Equal(3, roster.TotalCount); // Two known members and a loading marker.

        roster.GetRange(2, 2);
        await WaitUntilAsync(() => roster.IsComplete, changed);
        Assert.Equal([1, 2], requested);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], roster.GetRange(0, 10).Items);
    }

    [Fact]
    public async Task FullLastPageStopsAtReportedTotalWithoutRequestingAnExtraPage()
    {
        var changed = new SemaphoreSlim(0);
        var requested = new List<int>();
        using var roster = CreateRoster(
            (page, _) =>
            {
                requested.Add(page);
                return Task.FromResult(new PageResult<string>(["Able", "Baker", "Charlie"], 3));
            },
            (name, _) => Task.FromResult<string?>(name == "Charlie" ? null : name));
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 10);
        await WaitUntilAsync(() => roster.IsComplete, changed);

        Assert.Equal(["Able", "Baker"], roster.GetRange(0, 10).Items);
        Assert.Equal([1], requested);
    }

    private static ProgressiveFilteredRoster<string, string> CreateRoster(
        Func<int, CancellationToken, Task<PageResult<string>>> fetch,
        Func<string, CancellationToken, Task<string?>> resolve) =>
        new(3, fetch, resolve, "Loading", StringComparer.OrdinalIgnoreCase);

    private static async Task WaitUntilAsync(Func<bool> condition, SemaphoreSlim changed)
    {
        while (!condition())
        {
            await changed.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
