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

    [Fact]
    public async Task ScanToEnd_ChecksRemainingPagesWithoutScrollingAndPublishesEligibleMembers()
    {
        var changed = new SemaphoreSlim(0);
        var secondPage = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondPageStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new List<int>();
        using var roster = new ProgressiveFilteredRoster<string, string>(
            3,
            (page, _) =>
            {
                requested.Add(page);
                return Task.FromResult(new PageResult<string>(
                    page == 1 ? ["Able", "Dan", "Baker"] : ["Charlie"], 4));
            },
            (name, _) =>
            {
                if (name == "Charlie")
                {
                    secondPageStarted.SetResult();
                    return secondPage.Task;
                }

                return Task.FromResult<string?>(name == "Dan" ? null : name);
            },
            "Loading",
            StringComparer.OrdinalIgnoreCase,
            scanToEnd: true);
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 1);
        await secondPageStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(["Able", "Baker"], roster.LoadedItems);
        Assert.False(roster.IsComplete);
        Assert.Equal([1, 2], requested);

        secondPage.SetResult("Charlie");
        await WaitUntilAsync(() => roster.IsComplete, changed);
        Assert.Equal(["Able", "Baker", "Charlie"], roster.LoadedItems);
        Assert.Null(roster.Error);
    }

    [Fact]
    public async Task ScanToEnd_ReportsFailureOnLaterPageWithoutClaimingCompletion()
    {
        var changed = new SemaphoreSlim(0);
        using var roster = new ProgressiveFilteredRoster<string, string>(
            3,
            (page, _) => page == 1
                ? Task.FromResult(new PageResult<string>(["Able", "Baker", "Charlie"], 4))
                : Task.FromException<PageResult<string>>(new HttpRequestException("Next page failed")),
            (name, _) => Task.FromResult<string?>(name),
            "Loading",
            StringComparer.OrdinalIgnoreCase,
            scanToEnd: true);
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 1);
        await WaitUntilAsync(() => roster.Error is not null, changed);

        Assert.Equal(["Able", "Baker", "Charlie"], roster.LoadedItems);
        Assert.False(roster.IsComplete);
        Assert.IsType<HttpRequestException>(roster.Error);
    }

    [Fact]
    public async Task ScanToEnd_ReorderingKeepsSelectedMemberAndDoubleGradeChoice()
    {
        var changed = new SemaphoreSlim(0);
        var earlierMember = new TaskCompletionSource<TestMember?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selected = new TestMember("Zoey");
        using var roster = new ProgressiveFilteredRoster<string, TestMember>(
            2,
            (_, _) => Task.FromResult(new PageResult<string>(["Zoey", "Amy"], 2)),
            (name, _) => name == "Zoey" ? Task.FromResult<TestMember?>(selected) : earlierMember.Task,
            new TestMember("Loading"),
            Comparer<TestMember>.Create((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name)),
            scanToEnd: true);
        roster.Changed += () => changed.Release();

        roster.GetRange(0, 2);
        await WaitUntilAsync(() => roster.LoadedItems.Count == 1, changed);
        Assert.Same(selected, roster.GetRange(0, 2).Items[0]);
        selected.IsSelected = true;
        selected.IsDoubleGrading = true;

        earlierMember.SetResult(new TestMember("Amy"));
        await WaitUntilAsync(() => roster.IsComplete, changed);
        var reordered = roster.GetRange(0, 2).Items;
        Assert.Equal(["Amy", "Zoey"], reordered.Select(member => member.Name));
        Assert.Same(selected, reordered[1]);
        Assert.True(reordered[1].IsSelected);
        Assert.True(reordered[1].IsDoubleGrading);
    }

    private sealed class TestMember(string name)
    {
        public string Name { get; } = name;
        public bool IsSelected { get; set; }
        public bool IsDoubleGrading { get; set; }
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
