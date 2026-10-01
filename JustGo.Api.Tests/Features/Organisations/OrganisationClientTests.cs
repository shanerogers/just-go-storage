using System.Net;
using System.Text;
using JustGo.Api.Features.Organisations;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Tests.Features.Organisations;

public sealed class OrganisationClientTests
{
    [Fact]
    public async Task GetRolesAsync_RequestsPluralOrganisationsRoute()
    {
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[]}""", Encoding.UTF8, "application/json"),
            };
        }))
        {
            BaseAddress = new Uri("https://api.justgo.test"),
        };
        var client = new OrganisationClient(httpClient, Options.Create(new JustGoOptions
        {
            BaseUrl = "https://api.justgo.test",
            ApiKey = "test-key",
            ApiVersion = "v2.2",
        }));

        await client.GetRolesAsync(CancellationToken.None);

        Assert.NotNull(requestedUri);
        Assert.Equal("/api/v2.2/Organisations/Roles", requestedUri.AbsolutePath);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(send(request));
    }
}
