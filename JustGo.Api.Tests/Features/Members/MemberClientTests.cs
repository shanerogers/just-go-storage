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

    [Fact]
    public async Task GetMemberAsync_MapsOrganisationMembershipsAndRoles()
    {
        const string responseJson = """
            {
              "data": {
                "id": "e54f6726-597f-46d9-8341-1a89b59f3384",
                "organisations": [
                  {
                    "id": "4ab23821-e231-4492-9270-024fcbb2680a",
                    "organisationName": "Berhampore TKD",
                    "roles": "Club Admin",
                    "isPrimary": true,
                    "isAdmin": true
                  }
                ]
              }
            }
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
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

        var member = await client.GetMemberAsync(
            Guid.Parse("e54f6726-597f-46d9-8341-1a89b59f3384"),
            CancellationToken.None);

        var organisation = Assert.Single(member.Organisations!);
        Assert.Equal("Berhampore TKD", organisation.OrganisationName);
        Assert.Equal("Club Admin", organisation.Roles);
        Assert.True(organisation.IsAdmin);
        Assert.True(organisation.IsPrimary);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(send(request));
    }
}
