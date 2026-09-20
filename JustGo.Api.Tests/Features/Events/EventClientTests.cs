using System.Net;
using System.Text;
using JustGo.Api.Features.Events;
using JustGo.Integrations.JustGo.Features.Events.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Tests.Features.Events;

public sealed class EventClientTests
{
    [Fact]
    public async Task FindEventCandidatesByAttributesAsync_UsesEventIdQueryParameter()
    {
        var eventId = Guid.NewGuid();
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[]}""", Encoding.UTF8, "application/json"),
            };
        })
        {
            BaseAddress = new Uri("https://api.justgo.test"),
        };
        var client = new EventClient(httpClient, Options.Create(new JustGoOptions
        {
            BaseUrl = "https://api.justgo.test",
            ApiKey = "test-key",
            ApiVersion = "v2.2",
        }));

        await client.FindEventCandidatesByAttributesAsync(new FindEventCandidatesRequest
        {
            EventId = eventId,
            PageNumber = 2,
            PageSize = 50,
        }, CancellationToken.None);

        Assert.NotNull(requestedUri);
        Assert.Contains($"EventId={eventId}", requestedUri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("Id=", requestedUri.Query, StringComparison.Ordinal);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(send(request));
    }
}
