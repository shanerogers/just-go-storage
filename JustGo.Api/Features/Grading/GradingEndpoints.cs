using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Api.Features.Grading;

public static class GradingEndpoints
{
    private const int MaxConcurrentMemberDetailRequests = 20;
    private const int EventPageSize = 200;

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapGradingEndpoints()
        {
            var group = app.MapGroup("/grading").WithTags("Grading");

            group.MapGet("/grades", () => Results.Ok(GradeDefinitions.All))
                .WithName("GetGradeDefinitions")
                .WithSummary("Get all Gup and Dan grade definitions in rank order");

            group.MapGet("/members", GetGradingMembersAsync)
                .WithName("GetGradingMembers")
                .WithSummary("Get API members with current grade and eligibility for grading");

            group.MapGet("/events/{eventId:guid}/state", GetEventStateAsync)
                .WithName("GetGradingEventState")
                .WithSummary("Get current JustGo-backed grading state for an event");

            group.MapPost("/submit", SubmitGradingAsync)
                .WithName("SubmitGrading")
                .WithSummary("Create missing JustGo event bookings and issue credentials for graded members");

            return app;
        }
    }

    private static async Task<IResult> GetGradingMembersAsync(
        IMemberClient memberClient,
        CancellationToken ct,
        Guid? eventId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50)
    {
        if (pageSize is < 1 or > 200) pageSize = 50;
        if (page < 1) page = 1;

        if (eventId is null && string.IsNullOrWhiteSpace(search))
        {
            return Results.BadRequest("Either eventId or search must be provided.");
        }

        // JustGo only supports LastName search — extract the last word as the surname
        var searchTerm = search?.Trim();
        var lastName = searchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Contains(' '))
        {
            var parts = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            lastName = parts[^1];
        }

        var memberSearchRequest = new FindMembersRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            EventId = eventId,
            LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName,
        };

        var memberSearchResponse = await memberClient.FindMembersByAttributesAsync(memberSearchRequest, ct);
        var memberRows = memberSearchResponse.Data ?? [];

        // If search had multiple words, filter by first name(s) locally since JustGo only searches by LastName
        var firstName = !string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Contains(' ')
            ? string.Join(' ', searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries)[..^1])
            : null;

        var gradingMembers = memberRows
            .Where(m => firstName is null ||
                (m.FirstName is not null && m.FirstName.StartsWith(firstName, StringComparison.OrdinalIgnoreCase)))
            .Select(m => new GradingMemberDto
            {
                JustGoMemberId = m.Id,
                MemberId = m.MemberId ?? string.Empty,
                FirstName = m.FirstName ?? string.Empty,
                LastName = m.LastName ?? string.Empty,
            })
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToList();

        return Results.Ok(new GradingMembersResponse
        {
            Members = gradingMembers,
            TotalCount = memberSearchResponse.TotalRecords,
        });
    }

    private static async Task<IResult> GetEventStateAsync(
        Guid eventId,
        IEventClient eventClient,
        IMemberClient memberClient,
        CancellationToken ct)
    {
        var getTickets = GetAllEventTicketsAsync(eventClient, eventId, ct);
        var getCandidates = GetAllEventCandidatesAsync(eventClient, eventId, ct);

        await Task.WhenAll(getTickets, getCandidates);

        var candidates = await getCandidates;
        var tickets = await getTickets;

        var candidateMemberIds = candidates
            .Select(candidate => candidate.CandidateId)
            .Distinct()
            .ToList();

        var memberDetails = await LoadMemberDetailsAsync(candidateMemberIds, memberClient, ct);
        var memberDetailById = memberDetails.ToDictionary(member => member.Id);
        var ticketById = tickets.ToDictionary(ticket => ticket.Id);

        var response = new GradingEventStateResponse
        {
            EventId = eventId,
            Tickets = [.. tickets
                .Select(ToEventTicketState)
                .OrderBy(ticket => ticket.GradeName ?? ticket.TicketName)],
            Candidates = [.. candidates
                .Select(candidate => ToEventCandidateState(candidate, ticketById, memberDetailById))
                .OrderBy(candidate => candidate.LastName)
                .ThenBy(candidate => candidate.FirstName)],
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> SubmitGradingAsync(
        GradingSubmitRequest request,
        IEventClient eventClient,
        IMemberClient memberClient,
        ICredentialClient credentialClient,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (request.EventId == Guid.Empty)
        {
            return Results.BadRequest("A valid eventId is required.");
        }

        if (request.Results.Count == 0)
        {
            return Results.BadRequest("No grading results to submit.");
        }

        var logger = loggerFactory.CreateLogger("JustGo.Api.Features.Grading.GradingEndpoints");
        var tickets = await GetAllEventTicketsAsync(eventClient, request.EventId, ct);
        var ticketById = tickets.ToDictionary(ticket => ticket.Id);
        var existingCandidates = await GetAllEventCandidatesAsync(eventClient, request.EventId, ct);
        var existingBookingByMemberAndTicket = existingCandidates
            .GroupBy(candidate => (candidate.CandidateId, candidate.TicketId))
            .ToDictionary(group => group.Key, group => group.First());

        var memberDetails = await LoadMemberDetailsAsync(
            request.Results.Select(result => result.MemberId).Distinct(),
            memberClient,
            ct);

        var memberDetailById = memberDetails.ToDictionary(member => member.Id);

        var details = new List<GradingResultStatus>();
        var succeeded = 0;
        var failed = 0;

        foreach (var item in request.Results)
        {
            var gradeName = GradeDefinitions.GetGradeName(item.CredentialDefinitionId) ?? item.GradeName;
            var bookingId = item.BookingId;
            var bookingCreated = false;

            if (item.TicketId == Guid.Empty || !ticketById.ContainsKey(item.TicketId))
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Success = false,
                    Error = "The selected grade ticket was not found for this event.",
                });
                failed++;
                continue;
            }

            if (!memberDetailById.TryGetValue(item.MemberId, out var memberDetail))
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Success = false,
                    Error = "Failed to load member details from JustGo before issuing the credential.",
                });
                failed++;
                continue;
            }

            if (bookingId is null || bookingId == Guid.Empty)
            {
                if (existingBookingByMemberAndTicket.TryGetValue((item.MemberId, item.TicketId), out var existingCandidate))
                {
                    bookingId = existingCandidate.BookingId;
                }
                else
                {
                    var booking = await eventClient.AddEventCandidateAsync(new EventCandidateCreateRequest
                    {
                        MemberId = item.MemberId,
                        TicketId = item.TicketId,
                    }, ct);

                    bookingId = booking.BookingId;
                    bookingCreated = true;
                    existingBookingByMemberAndTicket[(item.MemberId, item.TicketId)] = new EventCandidateDto
                    {
                        CandidateId = item.MemberId,
                        TicketId = item.TicketId,
                        BookingId = booking.BookingId,
                    };
                }
            }

            var existingCredential = FindIssuedCredential(memberDetail.Credentials, item.CredentialDefinitionId);
            if (existingCredential is not null)
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    BookingId = bookingId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Success = false,
                    BookingCreated = bookingCreated,
                    SkippedDuplicate = true,
                    Error = "Member already has this active credential in JustGo.",
                    JustGoCredentialId = existingCredential.Id,
                });

                failed++;
                continue;
            }

            try
            {
                var createRequest = new MemberCredentialCreateRequest
                {
                    CredentialType = gradeName,
                    Value = gradeName,
                };

                var createdCredential = await credentialClient.CreateMemberCredentialAsync(item.MemberId, createRequest, ct);

                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    BookingId = bookingId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Success = true,
                    BookingCreated = bookingCreated,
                    JustGoCredentialId = createdCredential.CredentialId,
                });

                succeeded++;

                logger.LogInformation(
                    "Issued credential {GradeName} to member {MemberId} for event {EventName} ({EventId}) with booking {BookingId}.",
                    gradeName,
                    item.MemberId,
                    request.EventName,
                    request.EventId,
                    bookingId);
            }
            catch (Exception ex)
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    BookingId = bookingId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Success = false,
                    BookingCreated = bookingCreated,
                    Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                });

                failed++;

                logger.LogError(
                    ex,
                    "Failed to issue credential {GradeName} to member {MemberId} for event {EventId}.",
                    gradeName,
                    item.MemberId,
                    request.EventId);
            }
        }

        return Results.Ok(new GradingSubmitResponse
        {
            Succeeded = succeeded,
            Failed = failed,
            Details = details,
        });
    }

    private static async Task<MemberDetailDto[]> LoadMemberDetailsAsync(
        IEnumerable<Guid> memberIds,
        IMemberClient memberClient,
        CancellationToken ct)
    {
        var tasks = memberIds.Distinct().Select(memberId => LoadMemberDetailAsync(memberId, memberClient, ct));
        var results = await Task.WhenAll(tasks);
        return results.Where(result => result is not null).ToArray()!;
    }

    private static async Task<MemberDetailDto?> LoadMemberDetailAsync(
        Guid memberId,
        IMemberClient memberClient,
        CancellationToken ct)
    {
        return await memberClient.GetMemberAsync(memberId, ct);
    }

    private static async Task<List<EventTicketDto>> GetAllEventTicketsAsync(
        IEventClient eventClient,
        Guid eventId,
        CancellationToken ct)
    {
        var page = 1;
        var tickets = new List<EventTicketDto>();

        while (true)
        {
            var response = await eventClient.GetEventTicketsAsync(eventId, page, EventPageSize, ct);
            var batch = response.Data ?? [];
            tickets.AddRange(batch);

            if (batch.Count < EventPageSize)
            {
                return tickets;
            }

            page++;
        }
    }

    private static async Task<List<EventCandidateDto>> GetAllEventCandidatesAsync(
        IEventClient eventClient,
        Guid eventId,
        CancellationToken ct)
    {
        var page = 1;
        var candidates = new List<EventCandidateDto>();

        while (true)
        {
            var response = await eventClient.FindEventCandidatesByAttributesAsync(new FindEventCandidatesRequest
            {
                EventId = eventId,
                PageNumber = page,
                PageSize = EventPageSize,
            }, ct);

            var batch = response.Data ?? [];
            candidates.AddRange(batch);

            if (batch.Count < EventPageSize)
            {
                return candidates;
            }

            page++;
        }
    }

    private static GradingEventTicketDto ToEventTicketState(EventTicketDto ticket)
    {
        var grade = ResolveGradeDefinition(ticket.TicketName);

        return new GradingEventTicketDto
        {
            TicketId = ticket.Id,
            TicketName = ticket.TicketName ?? string.Empty,
            CredentialDefinitionId = grade?.DefinitionId,
            GradeName = grade?.Name,
            TotalBooked = ticket.TotalBooked,
            RemainingPlaces = ticket.RemainingPlaces,
            TicketCode = ticket.TicketCode,
            EndDate = ticket.EndDate,
        };
    }

    private static GradingEventCandidateDto ToEventCandidateState(
        EventCandidateDto candidate,
        IReadOnlyDictionary<Guid, EventTicketDto> ticketById,
        IReadOnlyDictionary<Guid, MemberDetailDto> memberDetailById)
    {
        ticketById.TryGetValue(candidate.TicketId, out var ticket);
        var grade = ResolveGradeDefinition(candidate.CourseName) ?? ResolveGradeDefinition(ticket?.TicketName);

        memberDetailById.TryGetValue(candidate.CandidateId, out var memberDetail);
        var issuedCredential = grade is null
            ? null
            : FindIssuedCredential(memberDetail?.Credentials, grade.DefinitionId);
        var currentGrade = memberDetail is not null ? GradeDefinitions.GetCurrentGrade(memberDetail.Credentials) : null;
        var lastGradingDate = memberDetail is not null ? GradeDefinitions.GetLastGradingDate(memberDetail.Credentials) : null;
        var nextGrade = currentGrade is not null
            ? GradeDefinitions.GetNextGrade(currentGrade.DefinitionId)
            : GradeDefinitions.All[0];
        var doubleGrade = currentGrade is not null
            ? GradeDefinitions.GetDoubleGrade(currentGrade.DefinitionId)
            : GradeDefinitions.All.Count > 1 ? GradeDefinitions.All[1] : null;

        return new GradingEventCandidateDto
        {
            BookingId = candidate.BookingId,
            MemberId = candidate.CandidateId,
            TicketId = candidate.TicketId,
            TicketName = candidate.CourseName ?? ticket?.TicketName ?? string.Empty,
            MemberNumber = candidate.MemberNumber ?? memberDetail?.MemberId ?? string.Empty,
            FirstName = candidate.FirstName ?? memberDetail?.FirstName ?? string.Empty,
            LastName = candidate.LastName ?? memberDetail?.LastName ?? string.Empty,
            CredentialDefinitionId = grade?.DefinitionId,
            GradeName = grade?.Name,
            BookingDate = candidate.BookingDate,
            HasIssuedCredential = issuedCredential is not null,
            JustGoCredentialId = issuedCredential?.Id,
            CredentialGrantedDate = NormalizeDate(issuedCredential?.GrantedDate),
            CurrentGrade = currentGrade?.Name,
            CurrentGradeDefinitionId = currentGrade?.DefinitionId,
            LastGradingDate = lastGradingDate,
            NextGrade = nextGrade?.Name,
            NextGradeDefinitionId = nextGrade?.DefinitionId,
            DoubleGrade = doubleGrade?.Name,
            DoubleGradeDefinitionId = doubleGrade?.DefinitionId,
        };
    }

    private static GradeDefinition? ResolveGradeDefinition(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return GradeDefinitions.All
            .OrderByDescending(grade => grade.Name.Length)
            .FirstOrDefault(grade => value.Contains(grade.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static MemberCredentialDtoV2_2? FindIssuedCredential(
        IEnumerable<MemberCredentialDtoV2_2>? credentials,
        Guid definitionId)
    {
        return credentials?.FirstOrDefault(credential =>
            credential.DefinitionId == definitionId &&
            string.Equals(credential.Status, "Active", StringComparison.OrdinalIgnoreCase));
    }

    private static DateOnly? NormalizeDate(DateOnly? value) =>
        value is null || value == DateOnly.MinValue ? null : value;
}
