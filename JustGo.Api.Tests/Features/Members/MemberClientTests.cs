using System.Net;
using System.Text;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Tests.Features.Members;

public sealed class MemberClientTests
{
    [Fact]
    public async Task FindMembersByAttributesAsync_WithClubId_SendsOrganisationIdQueryParameter()
    {
        var clubId = Guid.NewGuid();
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
        var client = new MemberClient(httpClient, Options.Create(new JustGoOptions
        {
            BaseUrl = "https://api.justgo.test",
            ApiKey = "test-key",
            ApiVersion = "v2.2",
        }));

        await client.FindMembersByAttributesAsync(new FindMembersRequest { ClubId = clubId }, CancellationToken.None);

        Assert.NotNull(requestedUri);
        Assert.Contains($"OrganisationId={clubId}", requestedUri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("ClubId=", requestedUri.Query, StringComparison.Ordinal);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(send(request));
    }
}
