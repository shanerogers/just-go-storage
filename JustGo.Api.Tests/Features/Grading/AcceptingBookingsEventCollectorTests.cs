using System.Text.Json;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Grading;
using JustGo.Integrations.JustGo.Features.Events.Models;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace JustGo.Api.Tests.Features.Grading;

public sealed class AcceptingBookingsEventCollectorTests
{
    private const string Accepting = AcceptingBookingsEventCollector.AcceptingBookingsStatus;
    private const int PageSize = AcceptingBookingsEventCollector.PageSize;

    [Fact]
    public async Task CollectAsync_KeepsOnlyAcceptingBookingsEvents()
    {
        var page = Events(("Accepting Bookings", 2), ("Complete", 3), ("Cancelled", 1), ("Closed for Bookings", 1));

        var result = await AcceptingBookingsEventCollector.CollectAsync(FromPages(page), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, evt => Assert.Equal(Accepting, evt.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task CollectAsync_StopsAtFirstPageShorterThanPageSize()
    {
        var requestedPages = new List<int>();
        var pages = new[]
        {
            Events((Accepting, 1), ("Complete", PageSize - 1)),
            Events(("Complete", PageSize)),
            Events((Accepting, 3), ("Draft", 2)),
            Events((Accepting, PageSize)),
        };

        var result = await AcceptingBookingsEventCollector.CollectAsync(
            (pageNumber, _) =>
            {
                requestedPages.Add(pageNumber);
                return Task.FromResult(pages[pageNumber - 1]);
            },
            CancellationToken.None);

        Assert.Equal([1, 2, 3], requestedPages);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task CollectAsync_WhenFirstPageIsEmpty_MakesOneRequest()
    {
        var calls = 0;

        var result = await AcceptingBookingsEventCollector.CollectAsync(
            (_, _) =>
            {
                calls++;
                return Task.FromResult<IReadOnlyList<JsonElement>>([]);
            },
            CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CollectAsync_WhenEveryPageIsFull_StopsAtMaxPages()
    {
        var calls = 0;
        var fullPage = Events(("Complete", PageSize));

        await AcceptingBookingsEventCollector.CollectAsync(
            (_, _) =>
            {
                calls++;
                return Task.FromResult(fullPage);
            },
            CancellationToken.None);

        Assert.Equal(AcceptingBookingsEventCollector.MaxPages, calls);
    }

    [Theory]
    [InlineData("""{"status":"Accepting Bookings"}""", true)]
    [InlineData("""{"status":" accepting bookings "}""", true)]
    [InlineData("""{"status":"Closed for Bookings"}""", false)]
    [InlineData("""{"status":null}""", false)]
    [InlineData("""{"eventName":"No status"}""", false)]
    public void IsAcceptingBookings_MatchesStatusIgnoringCaseAndWhitespace(string json, bool expected) =>
        Assert.Equal(expected, AcceptingBookingsEventCollector.IsAcceptingBookings(Parse(json)));

    [Fact]
    public void ReadEvents_ReturnsDataArray_OrEmptyForUnexpectedShapes()
    {
        Assert.Equal(2, AcceptingBookingsEventCollector.ReadEvents(Parse("""{"data":[{},{}]}""")).Count);
        Assert.Empty(AcceptingBookingsEventCollector.ReadEvents(Parse("""{"data":null}""")));
        Assert.Empty(AcceptingBookingsEventCollector.ReadEvents(Parse("[]")));
        Assert.Empty(AcceptingBookingsEventCollector.ReadEvents(null));
    }

    [Fact]
    public async Task GetAcceptingBookingsEventsAsync_RequestsGupGradingsTwentyAtATime()
    {
        var eventClient = Substitute.For<IEventClient>();
        var requests = new List<FindEventsRequest>();
        eventClient.FindEventsByAttributesAsync(Arg.Do<FindEventsRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult<object>(Response(Events((Accepting, 2), ("Complete", PageSize - 2)))),
                _ => Task.FromResult<object>(Response(Events((Accepting, 1), ("Template", 4)))));

        var result = await GradingEndpoints.GetAcceptingBookingsEventsAsync(eventClient, CancellationToken.None);

        Assert.Equal([1, 2], requests.Select(request => request.PageNumber));
        Assert.All(requests, request =>
        {
            Assert.Equal(PageSize, request.PageSize);
            Assert.Equal(EventCategory.GupGrading, request.Category);
        });

        var body = JsonSerializer.SerializeToElement(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
        Assert.Equal(3, body.GetProperty("totalRecords").GetInt32());
        Assert.Equal(3, body.GetProperty("data").GetArrayLength());
    }

    private static Func<int, CancellationToken, Task<IReadOnlyList<JsonElement>>> FromPages(
        params IReadOnlyList<JsonElement>[] pages) =>
        (pageNumber, _) => Task.FromResult(pageNumber <= pages.Length ? pages[pageNumber - 1] : (IReadOnlyList<JsonElement>)[]);

    private static IReadOnlyList<JsonElement> Events(params (string Status, int Count)[] groups) =>
        [.. groups.SelectMany(group => Enumerable.Range(0, group.Count)
            .Select(_ => JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), status = group.Status })))];

    private static JsonElement Response(IReadOnlyList<JsonElement> events) =>
        JsonSerializer.SerializeToElement(new { statusCode = 200, data = events });

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
