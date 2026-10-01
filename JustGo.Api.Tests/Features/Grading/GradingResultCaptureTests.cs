using System.Text.Json;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Grading;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace JustGo.Api.Tests.Features.Grading;

public sealed class GradingResultCaptureTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("P")]
    [InlineData("P-")]
    public async Task SubmitGradingAsync_AllowedOutcome_IsRecordedForTheCandidate(string outcome)
    {
        var request = CreateRequest(outcome);

        var response = await SubmitAsync(request);

        Assert.Equal(1, response.Succeeded);
        var detail = Assert.Single(response.Details);
        Assert.Equal(outcome, detail.Outcome);
    }

    [Fact]
    public async Task SubmitGradingAsync_TheoryMarkIsOptionalAndRecordedWhenSupplied()
    {
        var request = CreateRequest(GradingOutcomes.A, 87);

        var response = await SubmitAsync(request);

        var detail = Assert.Single(response.Details);
        Assert.Equal(87, detail.TheoryMark);
    }

    [Fact]
    public async Task SubmitGradingAsync_TheoryMarkOmitted_RemainsAbsentFromCandidateStatus()
    {
        var request = CreateRequest(GradingOutcomes.P);

        var response = await SubmitAsync(request);

        var detail = Assert.Single(response.Details);
        Assert.Null(detail.TheoryMark);
    }

    [Fact]
    public async Task SubmitGradingAsync_InvalidOutcome_IsRejectedExplicitlyForThatCandidate()
    {
        var request = CreateRequest("A+");

        var response = await SubmitAsync(request);

        Assert.Equal(0, response.Succeeded);
        Assert.Equal(1, response.Failed);
        var detail = Assert.Single(response.Details);
        Assert.False(detail.Success);
        Assert.Contains("outcome", detail.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task SubmitGradingAsync_OutOfRangeTheoryMark_IsRejectedExplicitlyForThatCandidate(int theoryMark)
    {
        var request = CreateRequest(GradingOutcomes.P, theoryMark);

        var response = await SubmitAsync(request);

        Assert.Equal(0, response.Succeeded);
        Assert.Equal(1, response.Failed);
        var detail = Assert.Single(response.Details);
        Assert.False(detail.Success);
        Assert.Contains("theory mark", detail.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GradingSubmitRequest_MalformedTheoryMark_IsRejectedByJsonBinding()
    {
        const string json = """
            {
              "eventId": "11111111-1111-1111-1111-111111111111",
              "eventName": "September grading",
              "eventDate": "2026-09-16",
              "results": [{
                "memberId": "22222222-2222-2222-2222-222222222222",
                "ticketId": "33333333-3333-3333-3333-333333333333",
                "gradeName": "5th Gup",
                "outcome": "A",
                "theoryMark": "not-a-number"
              }]
            }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<GradingSubmitRequest>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public async Task SubmitGradingAsync_InvalidCandidate_DoesNotHideOtherCandidateStatus()
    {
        var request = CreateRequest("A+");
        var second = CreateResult(request.Results[0].TicketId);
        request.Results.Add(second);

        var response = await SubmitAsync(request);

        Assert.Equal(1, response.Succeeded);
        Assert.Equal(1, response.Failed);
        Assert.Collection(
            response.Details,
            failed =>
            {
                Assert.False(failed.Success);
                Assert.Equal(request.Results[0].MemberId, failed.MemberId);
                Assert.Equal("A+", failed.Outcome);
            },
            succeeded =>
            {
                Assert.True(succeeded.Success);
                Assert.Equal(second.MemberId, succeeded.MemberId);
                Assert.Equal(GradingOutcomes.P, succeeded.Outcome);
            });
    }

    private static GradingSubmitRequest CreateRequest(
        string outcome = GradingOutcomes.P,
        int? theoryMark = null) => new()
    {
        EventId = Guid.NewGuid(),
        EventName = "September grading",
        EventDate = new DateOnly(2026, 9, 16),
        Results = [CreateResult(outcome: outcome, theoryMark: theoryMark)],
    };

    private static GradingResultItem CreateResult(
        Guid? ticketId = null,
        string outcome = GradingOutcomes.P,
        int? theoryMark = null) => new()
    {
        MemberId = Guid.NewGuid(),
        TicketId = ticketId ?? Guid.NewGuid(),
        BookingId = Guid.NewGuid(),
        MemberNumber = "MID-123",
        GradeName = "5th Gup",
        Outcome = outcome,
        TheoryMark = theoryMark,
    };

    private static async Task<GradingSubmitResponse> SubmitAsync(GradingSubmitRequest request)
    {
        var result = await SubmitRawAsync(request);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        return Assert.IsType<GradingSubmitResponse>(valueResult.Value);
    }

    private static Task<IResult> SubmitRawAsync(GradingSubmitRequest request)
    {
        var eventClient = Substitute.For<IEventClient>();
        eventClient.GetEventTicketsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new EventCollectionResponse<EventTicketDto>
            {
                Data = [new EventTicketDto { Id = request.Results[0].TicketId, TicketName = "5th Gup" }],
            });
        eventClient.FindEventCandidatesByAttributesAsync(Arg.Any<FindEventCandidatesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCollectionResponse<EventCandidateDto> { Data = [] });

        var memberClient = Substitute.For<IMemberClient>();
        foreach (var item in request.Results)
        {
            memberClient.GetMemberAsync(item.MemberId, Arg.Any<CancellationToken>())
                .Returns(new MemberDetailDto { Id = item.MemberId, Credentials = [] });
        }

        var credentialClient = Substitute.For<ICredentialClient>();
        credentialClient.CreateMemberCredentialAsync(Arg.Any<Guid>(), Arg.Any<MemberCredentialCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new MemberCredentialCreatedResponse { CredentialId = Guid.NewGuid() });

        return GradingEndpoints.SubmitGradingAsync(
            request,
            eventClient,
            memberClient,
            credentialClient,
            NullLoggerFactory.Instance,
            CancellationToken.None);
    }

}
