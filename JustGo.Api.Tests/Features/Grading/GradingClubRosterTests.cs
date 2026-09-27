using System.Text.Json;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Grading;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace JustGo.Api.Tests.Features.Grading;

public sealed class GradingClubRosterTests
{
    [Fact]
    public async Task GetGradingClubsAsync_ReturnsOnlyActiveClubsSortedByName()
    {
        var zebraId = Guid.NewGuid();
        var avondaleId = Guid.NewGuid();
        var clubClient = Substitute.For<IClubClient>();
        clubClient.FindClubsByAttributesAsync(Arg.Any<FindClubsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ClubPage(
                Club(zebraId, "Zebra TKD", "Club", "Active"),
                Club(Guid.NewGuid(), "Dormant TKD", "Club", "Inactive"),
                Club(Guid.NewGuid(), "Auckland Region", "Region", "Active"),
                Club(avondaleId, "Avondale TKD", "Club", "Active")));

        var clubs = await GetClubsAsync(clubClient);

        Assert.Collection(
            clubs,
            first => Assert.Equal(avondaleId, first.Id),
            second => Assert.Equal(zebraId, second.Id));
    }

    [Fact]
    public async Task GetGradingClubsAsync_WhenFirstPageIsFull_ReadsNextPage()
    {
        var lastClubId = Guid.NewGuid();
        var fullPage = Enumerable.Range(0, 100)
            .Select(index => Club(Guid.NewGuid(), $"Club {index:D3}", "Club", "Active"))
            .ToArray();
        var clubClient = Substitute.For<IClubClient>();
        clubClient.FindClubsByAttributesAsync(Arg.Is<FindClubsRequest>(r => r.PageNumber == 1), Arg.Any<CancellationToken>())
            .Returns(ClubPage(fullPage));
        clubClient.FindClubsByAttributesAsync(Arg.Is<FindClubsRequest>(r => r.PageNumber == 2), Arg.Any<CancellationToken>())
            .Returns(ClubPage(Club(lastClubId, "Zulu TKD", "Club", "Active")));

        var clubs = await GetClubsAsync(clubClient);

        Assert.Equal(101, clubs.Count);
        Assert.Equal(lastClubId, clubs[^1].Id);
        await clubClient.DidNotReceive().FindClubsByAttributesAsync(
            Arg.Is<FindClubsRequest>(r => r.PageNumber == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGradingClubsAsync_WhenCached_DoesNotCallJustGoAgain()
    {
        var clubClient = Substitute.For<IClubClient>();
        clubClient.FindClubsByAttributesAsync(Arg.Any<FindClubsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ClubPage(Club(Guid.NewGuid(), "Avondale TKD", "Club", "Active")));
        var cache = NewCache();

        var first = await GetClubsAsync(clubClient, cache);
        var second = await GetClubsAsync(clubClient, cache);

        Assert.Equal(first.Single().Id, second.Single().Id);
        await clubClient.Received(1).FindClubsByAttributesAsync(Arg.Any<FindClubsRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGradingClubsAsync_WhenNoClubsFound_DoesNotCacheEmptyList()
    {
        var clubClient = Substitute.For<IClubClient>();
        clubClient.FindClubsByAttributesAsync(Arg.Any<FindClubsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ClubPage());
        var cache = NewCache();

        await GetClubsAsync(clubClient, cache);

        Assert.Null(await cache.GetStringAsync(GradingEndpoints.ClubListCacheKey));
    }

    [Fact]
    public async Task GetGradingMembersAsync_WithClubOnly_ListsClubMembersPage()
    {
        var clubId = Guid.NewGuid();
        var memberClient = Substitute.For<IMemberClient>();
        memberClient.FindMembersByAttributesAsync(Arg.Any<FindMembersRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MembersPagedResponse
            {
                TotalRecords = 43,
                Data = [new JustGoMemberDto { Id = Guid.NewGuid(), FirstName = "Aaron", LastName = "Guo" }],
            });

        var result = await GradingEndpoints.GetGradingMembersAsync(
            memberClient,
            CancellationToken.None,
            clubId: clubId,
            page: 2,
            pageSize: 25);

        var response = Assert.IsType<GradingMembersResponse>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
        Assert.Equal(43, response.TotalCount);
        Assert.Single(response.Members);
        await memberClient.Received(1).FindMembersByAttributesAsync(
            Arg.Is<FindMembersRequest>(request =>
                request.ClubId == clubId &&
                request.LastName == null &&
                request.PageNumber == 2 &&
                request.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGradingMembersAsync_WithoutEventClubOrSearch_ReturnsBadRequest()
    {
        var result = await GradingEndpoints.GetGradingMembersAsync(Substitute.For<IMemberClient>(), CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    private static async Task<List<GradingClubDto>> GetClubsAsync(IClubClient clubClient, IDistributedCache? cache = null)
    {
        var result = await GradingEndpoints.GetGradingClubsAsync(clubClient, cache ?? NewCache(), CancellationToken.None);
        return Assert.IsType<List<GradingClubDto>>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
    }

    private static MemoryDistributedCache NewCache() =>
        new(Options.Create(new MemoryDistributedCacheOptions()));

    private static object ClubPage(params object[] clubs) =>
        JsonSerializer.SerializeToElement(new { data = clubs });

    private static object Club(Guid id, string name, string type, string status) =>
        new { id, organisationName = name, organisationType = type, organisationStatus = status, organisationTown = "Auckland" };
}
