using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Grading;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Events.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JustGo.Api.Tests.Features.Grading;

public sealed class GradingEnrolmentTests
{
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid FifthGupTicketId = Guid.NewGuid();
    private static readonly Guid FourthGupTicketId = Guid.NewGuid();

    [Fact]
    public void MapGradingEndpoints_RegistersEnrolmentRoute()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(Substitute.For<IEventClient>());
        builder.Services.AddSingleton(Substitute.For<IMemberClient>());
        builder.Services.AddSingleton(Substitute.For<ICredentialClient>());
        builder.Services.AddSingleton(Substitute.For<JustGo.Api.Features.Clubs.IClubClient>());
        builder.Services.AddDistributedMemoryCache();
        var application = builder.Build();

        application.MapGradingEndpoints();

        var routePatterns = ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText);

        Assert.Contains("/grading/events/{eventId:guid}/enrolments", routePatterns);
    }

    [Fact]
    public async Task EnrolMembersAsync_WithSelectedTicket_BooksMemberOntoThatTicket()
    {
        var memberId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var eventClient = CreateEventClient();
        eventClient.AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCandidateCreatedResponse { BookingId = bookingId });
        var memberClient = Substitute.For<IMemberClient>();

        var response = await EnrolAsync(
            [new GradingEnrolmentItem { MemberId = memberId, TicketId = FourthGupTicketId }],
            eventClient,
            memberClient);

        Assert.Equal(1, response.Enrolled);
        var detail = Assert.Single(response.Details);
        Assert.Equal(GradingEnrolmentOutcome.Enrolled, detail.Outcome);
        Assert.Equal(bookingId, detail.BookingId);
        Assert.Equal("4th Gup", detail.GradeName);
        await eventClient.Received(1).AddEventCandidateAsync(
            Arg.Is<EventCandidateCreateRequest>(request => request.MemberId == memberId && request.TicketId == FourthGupTicketId),
            Arg.Any<CancellationToken>());
        await memberClient.DidNotReceive().GetMemberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrolMembersAsync_WithoutTicket_UsesTicketMatchingMembersNextGrade()
    {
        var memberId = Guid.NewGuid();
        var eventClient = CreateEventClient();
        eventClient.AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCandidateCreatedResponse { BookingId = Guid.NewGuid() });
        var memberClient = CreateMemberClientWithGrade(memberId, "6th Gup");

        var response = await EnrolAsync([new GradingEnrolmentItem { MemberId = memberId }], eventClient, memberClient);

        var detail = Assert.Single(response.Details);
        Assert.Equal(GradingEnrolmentOutcome.Enrolled, detail.Outcome);
        Assert.Equal(FifthGupTicketId, detail.TicketId);
        await eventClient.Received(1).AddEventCandidateAsync(
            Arg.Is<EventCandidateCreateRequest>(request => request.TicketId == FifthGupTicketId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrolMembersAsync_WhenMemberAlreadyBooked_SkipsWithoutCreatingBooking()
    {
        var memberId = Guid.NewGuid();
        var existingBookingId = Guid.NewGuid();
        var eventClient = CreateEventClient(new EventCandidateDto
        {
            CandidateId = memberId,
            BookingId = existingBookingId,
            TicketId = FifthGupTicketId,
        });

        var response = await EnrolAsync(
            [new GradingEnrolmentItem { MemberId = memberId, TicketId = FourthGupTicketId }],
            eventClient,
            Substitute.For<IMemberClient>());

        Assert.Equal(0, response.Enrolled);
        Assert.Equal(1, response.AlreadyEnrolled);
        var detail = Assert.Single(response.Details);
        Assert.Equal(GradingEnrolmentOutcome.AlreadyEnrolled, detail.Outcome);
        Assert.Equal(existingBookingId, detail.BookingId);
        Assert.Equal("5th Gup", detail.GradeName);
        await eventClient.DidNotReceive().AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrolMembersAsync_WhenMemberRepeatedInRequest_BooksOnlyOnce()
    {
        var memberId = Guid.NewGuid();
        var eventClient = CreateEventClient();
        eventClient.AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCandidateCreatedResponse { BookingId = Guid.NewGuid() });

        var response = await EnrolAsync(
            [
                new GradingEnrolmentItem { MemberId = memberId, TicketId = FifthGupTicketId },
                new GradingEnrolmentItem { MemberId = memberId, TicketId = FifthGupTicketId },
            ],
            eventClient,
            Substitute.For<IMemberClient>());

        Assert.Equal(1, response.Enrolled);
        Assert.Equal(1, response.AlreadyEnrolled);
        await eventClient.Received(1).AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrolMembersAsync_WhenTicketNotInEvent_ReportsFailureWithoutBooking()
    {
        var eventClient = CreateEventClient();

        var response = await EnrolAsync(
            [new GradingEnrolmentItem { MemberId = Guid.NewGuid(), TicketId = Guid.NewGuid() }],
            eventClient,
            Substitute.For<IMemberClient>());

        var detail = Assert.Single(response.Details);
        Assert.Equal(GradingEnrolmentOutcome.Failed, detail.Outcome);
        Assert.Contains("ticket was not found", detail.Error, StringComparison.OrdinalIgnoreCase);
        await eventClient.DidNotReceive().AddEventCandidateAsync(Arg.Any<EventCandidateCreateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrolMembersAsync_WhenNoTicketMatchesNextGrade_AsksAdminToChooseTicket()
    {
        var memberId = Guid.NewGuid();
        var eventClient = CreateEventClient();
        var memberClient = CreateMemberClientWithGrade(memberId, "1st Gup");

        var response = await EnrolAsync([new GradingEnrolmentItem { MemberId = memberId }], eventClient, memberClient);

        var detail = Assert.Single(response.Details);
        Assert.Equal(GradingEnrolmentOutcome.Failed, detail.Outcome);
        Assert.Contains("1st Dan", detail.Error);
        Assert.Contains("Choose a ticket", detail.Error);
    }

    [Fact]
    public async Task EnrolMembersAsync_WhenOneBookingFails_ContinuesWithRemainingMembers()
    {
        var failingMemberId = Guid.NewGuid();
        var succeedingMemberId = Guid.NewGuid();
        var eventClient = CreateEventClient();
        eventClient.AddEventCandidateAsync(
                Arg.Is<EventCandidateCreateRequest>(request => request.MemberId == failingMemberId),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Ticket is sold out."));
        eventClient.AddEventCandidateAsync(
                Arg.Is<EventCandidateCreateRequest>(request => request.MemberId == succeedingMemberId),
                Arg.Any<CancellationToken>())
            .Returns(new EventCandidateCreatedResponse { BookingId = Guid.NewGuid() });

        var response = await EnrolAsync(
            [
                new GradingEnrolmentItem { MemberId = failingMemberId, TicketId = FifthGupTicketId },
                new GradingEnrolmentItem { MemberId = succeedingMemberId, TicketId = FifthGupTicketId },
            ],
            eventClient,
            Substitute.For<IMemberClient>());

        Assert.Equal(1, response.Enrolled);
        Assert.Equal(1, response.Failed);
        Assert.Collection(
            response.Details,
            failed =>
            {
                Assert.Equal(GradingEnrolmentOutcome.Failed, failed.Outcome);
                Assert.Equal("Ticket is sold out.", failed.Error);
            },
            succeeded => Assert.Equal(GradingEnrolmentOutcome.Enrolled, succeeded.Outcome));
    }

    [Fact]
    public async Task EnrolMembersAsync_WithNoMembers_ReturnsBadRequest()
    {
        var result = await GradingEndpoints.EnrolMembersAsync(
            EventId,
            new GradingEnrolmentRequest(),
            CreateEventClient(),
            Substitute.For<IMemberClient>(),
            NullLoggerFactory.Instance,
            CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    private static IEventClient CreateEventClient(params EventCandidateDto[] existingCandidates)
    {
        var eventClient = Substitute.For<IEventClient>();
        eventClient.GetEventTicketsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new EventCollectionResponse<EventTicketDto>
            {
                Data =
                [
                    new EventTicketDto { Id = FifthGupTicketId, TicketName = "5th Gup Grading" },
                    new EventTicketDto { Id = FourthGupTicketId, TicketName = "4th Gup Grading" },
                ],
            });
        eventClient.FindEventCandidatesByAttributesAsync(Arg.Any<FindEventCandidatesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventCollectionResponse<EventCandidateDto> { Data = [.. existingCandidates] });
        return eventClient;
    }

    private static IMemberClient CreateMemberClientWithGrade(Guid memberId, string currentGrade)
    {
        var memberClient = Substitute.For<IMemberClient>();
        memberClient.GetMemberAsync(memberId, Arg.Any<CancellationToken>())
            .Returns(new MemberDetailDto
            {
                Id = memberId,
                Credentials = [new MemberCredentialDtoV2_2 { Name = currentGrade, Status = "Active" }],
            });
        return memberClient;
    }

    private static async Task<GradingEnrolmentResponse> EnrolAsync(
        List<GradingEnrolmentItem> members,
        IEventClient eventClient,
        IMemberClient memberClient)
    {
        var result = await GradingEndpoints.EnrolMembersAsync(
            EventId,
            new GradingEnrolmentRequest { Members = members },
            eventClient,
            memberClient,
            NullLoggerFactory.Instance,
            CancellationToken.None);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        return Assert.IsType<GradingEnrolmentResponse>(valueResult.Value);
    }
}
