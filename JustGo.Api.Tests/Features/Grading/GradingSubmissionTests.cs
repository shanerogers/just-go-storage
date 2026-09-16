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

public sealed class GradingSubmissionTests
{
    [Fact]
    public async Task SubmitGradingAsync_WhenCredentialAlreadyExists_SkipsDuplicateWithoutCreatingCredential()
    {
        var memberId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var eventClient = CreateEventClient(ticketId);
        var memberClient = CreateMemberClient(memberId, [new MemberCredentialDtoV2_2
        {
            Id = credentialId,
            Name = "5th Gup",
            Status = "Active",
        }]);
        var credentialClient = Substitute.For<ICredentialClient>();

        var response = await SubmitAsync(CreateRequest(memberId, ticketId, Guid.NewGuid()), eventClient, memberClient, credentialClient);

        Assert.Equal(0, response.Succeeded);
        Assert.Equal(1, response.Failed);
        var detail = Assert.Single(response.Details);
        Assert.True(detail.SkippedDuplicate);
        Assert.Equal(credentialId, detail.JustGoCredentialId);
        await credentialClient.DidNotReceive().CreateMemberCredentialAsync(Arg.Any<Guid>(), Arg.Any<MemberCredentialCreateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitGradingAsync_WhenBookingIsMissing_CreatesBookingBeforeCredential()
    {
        var memberId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var eventClient = CreateEventClient(ticketId);
        eventClient.AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCandidateCreatedResponse { BookingId = bookingId });
        var memberClient = CreateMemberClient(memberId);
        var credentialClient = Substitute.For<ICredentialClient>();
        credentialClient.CreateMemberCredentialAsync(Arg.Any<Guid>(), Arg.Any<MemberCredentialCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemberCredentialCreatedResponse { CredentialId = credentialId });

        var response = await SubmitAsync(CreateRequest(memberId, ticketId), eventClient, memberClient, credentialClient);

        Assert.Equal(1, response.Succeeded);
        Assert.Equal(0, response.Failed);
        var detail = Assert.Single(response.Details);
        Assert.True(detail.BookingCreated);
        Assert.Equal(bookingId, detail.BookingId);
        Assert.Equal(credentialId, detail.JustGoCredentialId);
        await eventClient.Received(1).AddEventCandidateAsync(
            Arg.Is<EventCandidateCreateRequest>(request => request.MemberId == memberId && request.TicketId == ticketId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitGradingAsync_WhenOneCredentialFails_ReturnsBothSuccessAndFailureDetails()
    {
        var firstMemberId = Guid.NewGuid();
        var secondMemberId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var eventClient = CreateEventClient(ticketId);
        var memberClient = CreateMemberClient(firstMemberId, secondMemberId);
        var credentialClient = Substitute.For<ICredentialClient>();
        credentialClient.CreateMemberCredentialAsync(firstMemberId, Arg.Any<MemberCredentialCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemberCredentialCreatedResponse { CredentialId = Guid.NewGuid() });
        credentialClient.CreateMemberCredentialAsync(secondMemberId, Arg.Any<MemberCredentialCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MemberCredentialCreatedResponse>(
                new InvalidOperationException("Upstream credential service rejected this member.")));

        var request = CreateRequest(firstMemberId, ticketId, Guid.NewGuid());
        request.Results.Add(CreateResult(secondMemberId, ticketId, Guid.NewGuid()));

        var response = await SubmitAsync(request, eventClient, memberClient, credentialClient);

        Assert.Equal(1, response.Succeeded);
        Assert.Equal(1, response.Failed);
        Assert.Collection(
            response.Details,
            succeeded => Assert.True(succeeded.Success),
            failed =>
            {
                Assert.False(failed.Success);
                Assert.Equal("Upstream credential service rejected this member.", failed.Error);
            });
    }

    private static IEventClient CreateEventClient(Guid ticketId)
    {
        var eventClient = Substitute.For<IEventClient>();
        eventClient.GetEventTicketsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new EventCollectionResponse<EventTicketDto>
            {
                Data = [new EventTicketDto { Id = ticketId, TicketName = "5th Gup" }],
            });
        eventClient.FindEventCandidatesByAttributesAsync(Arg.Any<FindEventCandidatesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCollectionResponse<EventCandidateDto> { Data = [] });
        return eventClient;
    }

    private static IMemberClient CreateMemberClient(Guid firstMemberId, Guid? secondMemberId = null)
    {
        var memberIds = new List<Guid> { firstMemberId };
        if (secondMemberId is { } secondId)
        {
            memberIds.Add(secondId);
        }

        return CreateMemberClient(memberIds, []);
    }

    private static IMemberClient CreateMemberClient(Guid memberId, List<MemberCredentialDtoV2_2> credentials) =>
        CreateMemberClient([memberId], credentials);

    private static IMemberClient CreateMemberClient(IEnumerable<Guid> memberIds, List<MemberCredentialDtoV2_2> credentials)
    {
        var memberClient = Substitute.For<IMemberClient>();
        foreach (var memberId in memberIds)
        {
            memberClient.GetMemberAsync(memberId, Arg.Any<CancellationToken>())
                .Returns(new MemberDetailDto { Id = memberId, Credentials = credentials });
        }

        return memberClient;
    }

    private static GradingSubmitRequest CreateRequest(Guid memberId, Guid ticketId, Guid? bookingId = null) =>
        new()
        {
            EventId = Guid.NewGuid(),
            EventName = "September grading",
            EventDate = new DateOnly(2026, 9, 16),
            Results = [CreateResult(memberId, ticketId, bookingId)],
        };

    private static GradingResultItem CreateResult(Guid memberId, Guid ticketId, Guid? bookingId) =>
        new()
        {
            MemberId = memberId,
            TicketId = ticketId,
            BookingId = bookingId,
            MemberNumber = "MID-123",
            GradeName = "5th Gup",
        };

    private static async Task<GradingSubmitResponse> SubmitAsync(
        GradingSubmitRequest request,
        IEventClient eventClient,
        IMemberClient memberClient,
        ICredentialClient credentialClient)
    {
        var result = await GradingEndpoints.SubmitGradingAsync(
            request,
            eventClient,
            memberClient,
            credentialClient,
            NullLoggerFactory.Instance,
            CancellationToken.None);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        return Assert.IsType<GradingSubmitResponse>(valueResult.Value);
    }
}
