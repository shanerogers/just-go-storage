using System.Text.Json;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace JustGo.Api.Features.Grading;

public static class GradingEndpoints
{
    private static readonly EventCategory[] GradingCategories =
        [EventCategory.GupGrading];

    private const int MaxConcurrentMemberDetailRequests = 20;
    private const int EventPageSize = 200;

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapGradingEndpoints()
        {
            var group = app.MapGroup("/grading").WithTags("Grading");

            group.MapGet("/grades", () => Results.Ok(Grade.All.Select(g => new { g.Name, g.Rank, g.IsGup, g.IsDan })))
                .WithName("GetGradeDefinitions")
                .WithSummary("Get all Gup and Dan grade definitions in rank order");

            group.MapGet("/events", GetGradingEventsAsync)
                .WithName("GetGradingEvents")
                .WithSummary("Search grading events (Gup Grading, Dan Grading, Dan Pass Incomplete)");

            group.MapGet("/events/accepting-bookings", GetAcceptingBookingsEventsAsync)
                .WithName("GetAcceptingBookingsGradingEvents")
                .WithSummary("List Gup gradings with status 'Accepting Bookings', paging JustGo 100 events at a time");

            group.MapGet("/members", GetGradingMembersAsync)
                .WithName("GetGradingMembers")
                .WithSummary("Search members by name, event or club (paged) — fast, returns basic info only (no grades)");

            group.MapGet("/clubs", GetGradingClubsAsync)
                .WithName("GetGradingClubs")
                .WithSummary("List active clubs, sorted by name, for choosing whose members to enrol (cached 12h)");

            group.MapGet("/events/{eventId:guid}/tickets", GetEventTicketsAsync)
                .WithName("GetGradingEventTickets")
                .WithSummary("Get grade ticket slots for an event — fast");

            group.MapGet("/events/{eventId:guid}/candidates", GetEventCandidatesAsync)
                .WithName("GetGradingEventCandidates")
                .WithSummary("Get booked members for an event — fast, basic info from booking");

            group.MapPost("/members/details", GetMemberDetailsAsync)
                .WithName("GetGradingMemberDetails")
                .WithSummary("Batch-fetch member credentials and resolve grades — slow, call separately");

            group.MapGet("/members/{memberId:guid}/details", GetSingleMemberDetailsAsync)
                .WithName("GetGradingSingleMemberDetails")
                .WithSummary("Fetch a single member's credentials and resolve grades");

            group.MapGet("/events/{eventId:guid}/state", GetEventStateAsync)
                .WithName("GetGradingEventState")
                .WithSummary("Get current JustGo-backed grading state for an event (legacy monolithic)");

            group.MapPost("/submit", SubmitGradingAsync)
                .WithName("SubmitGrading")
                .WithSummary("Capture grading outcomes, create missing JustGo event bookings, and issue credentials");

            // Intended for club admins; access control is not yet enforced (role TBD).
            group.MapPost("/events/{eventId:guid}/enrolments", EnrolMembersAsync)
                .WithName("EnrolGradingMembers")
                .WithSummary("Book members onto Gup grade tickets (including requested double grades), skipping those already booked");

            return app;
        }
    }

    private static async Task<IResult> GetGradingEventsAsync(
        IEventClient eventClient,
        CancellationToken ct,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 200)
    {
        var isEventNumber = search is not null &&
            search.StartsWith("EV", StringComparison.OrdinalIgnoreCase);

        var request = new FindEventsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            EventNumber = isEventNumber ? search : null,
            Category = GradingCategories[0],
        };

        var result = await eventClient.FindEventsByAttributesAsync(request, ct);

        // EventName filter is ignored by JustGo API, so filter locally by name
        if (!isEventNumber && !string.IsNullOrWhiteSpace(search))
        {
            result = FilterEventsByName(result, search);
        }

        return Results.Ok(result);
    }

    internal static async Task<IResult> GetAcceptingBookingsEventsAsync(IEventClient eventClient, CancellationToken ct)
    {
        var events = await AcceptingBookingsEventCollector.CollectAsync(
            async (pageNumber, token) =>
            {
                var request = new FindEventsRequest
                {
                    PageNumber = pageNumber,
                    PageSize = AcceptingBookingsEventCollector.PageSize,
                    Category = GradingCategories[0],
                };
                var response = await eventClient.FindEventsByAttributesAsync(request, token);
                return AcceptingBookingsEventCollector.ReadEvents(response);
            },
            ct);

        return Results.Ok(new { totalRecords = events.Count, data = events });
    }

    private static object FilterEventsByName(object result, string search)
    {
        if (result is not JsonElement json)
        {
            return result;
        }

        if (!json.TryGetProperty("data", out var data))
        {
            return result;
        }

        var filtered = data.EnumerateArray()
            .Where(e => e.TryGetProperty("eventName", out var name) &&
                        name.GetString()?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        return new
        {
            statusCode = json.TryGetProperty("statusCode", out var sc) ? sc.GetInt32() : 200,
            message = json.TryGetProperty("message", out var msg) ? msg.GetString() : null,
            pageNumber = json.TryGetProperty("pageNumber", out var pn) ? pn.GetInt32() : 1,
            pageSize = json.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : 200,
            totalPages = 1,
            totalRecords = filtered.Count,
            data = filtered,
        };
    }

    internal static async Task<IResult> GetGradingMembersAsync(
        IMemberClient memberClient,
        CancellationToken ct,
        Guid? eventId = null,
        Guid? clubId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50)
    {
        if (pageSize is < 1 or > 200)
        {
            pageSize = 50;
        }

        if (page < 1)
        {
            page = 1;
        }

        if (eventId is null && clubId is null && string.IsNullOrWhiteSpace(search))
        {
            return Results.BadRequest("Either eventId, clubId or search must be provided.");
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
            ClubId = clubId,
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
                MemberId = m.MemberNumber ?? m.MemberId ?? string.Empty,
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

    private const int ClubPageSize = 100;
    private const int MaxClubPages = 50;
    internal const string ClubListCacheKey = "grading:clubs:active:v1";
    private static readonly DistributedCacheEntryOptions ClubListCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
    };

    // JustGo's organisation search is slow and flaky, and the club list rarely changes, so it's cached.
    internal static async Task<IResult> GetGradingClubsAsync(
        IClubClient clubClient,
        IDistributedCache cache,
        CancellationToken ct)
    {
        var cached = await cache.GetStringAsync(ClubListCacheKey, ct);
        if (cached is not null && JsonSerializer.Deserialize<List<GradingClubDto>>(cached) is { Count: > 0 } cachedClubs)
        {
            return Results.Ok(cachedClubs);
        }

        var clubs = await FetchActiveClubsAsync(clubClient, ct);
        if (clubs.Count > 0)
        {
            await cache.SetStringAsync(ClubListCacheKey, JsonSerializer.Serialize(clubs), ClubListCacheOptions, ct);
        }

        return Results.Ok(clubs);
    }

    private static async Task<List<GradingClubDto>> FetchActiveClubsAsync(IClubClient clubClient, CancellationToken ct)
    {
        var clubs = new List<GradingClubDto>();

        for (var pageNumber = 1; pageNumber <= MaxClubPages; pageNumber++)
        {
            var response = await clubClient.FindClubsByAttributesAsync(
                new FindClubsRequest { PageNumber = pageNumber, PageSize = ClubPageSize },
                ct);
            var (pageClubs, rowCount) = ParseActiveClubs(response);
            clubs.AddRange(pageClubs);

            if (rowCount < ClubPageSize)
            {
                break;
            }
        }

        return clubs
            .DistinctBy(club => club.Id)
            .OrderBy(club => club.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (List<GradingClubDto> Clubs, int RowCount) ParseActiveClubs(object response)
    {
        if (response is not JsonElement json ||
            !json.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array)
        {
            return ([], 0);
        }

        var clubs = data.EnumerateArray()
            .Where(IsActiveClub)
            .Select(item => new GradingClubDto
            {
                Id = item.TryGetProperty("id", out var id) && id.TryGetGuid(out var clubId) ? clubId : Guid.Empty,
                Name = GetJsonString(item, "organisationName"),
                Town = GetJsonString(item, "organisationTown"),
            })
            .Where(club => club.Id != Guid.Empty && !string.IsNullOrWhiteSpace(club.Name))
            .ToList();

        return (clubs, data.GetArrayLength());
    }

    private static bool IsActiveClub(JsonElement item) =>
        string.Equals(GetJsonString(item, "organisationType"), "Club", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(GetJsonString(item, "organisationStatus"), "Active", StringComparison.OrdinalIgnoreCase);

    private static string GetJsonString(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static async Task<IResult> GetEventTicketsAsync(
        Guid eventId,
        IEventClient eventClient,
        CancellationToken ct)
    {
        var tickets = await GetAllEventTicketsAsync(eventClient, eventId, ct);

        var result = tickets
            .Select(ToEventTicketState)
            .OrderBy(t => t.GradeName ?? t.TicketName)
            .ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetEventCandidatesAsync(
        Guid eventId,
        IEventClient eventClient,
        CancellationToken ct)
    {
        var candidates = await GetAllEventCandidatesAsync(eventClient, eventId, ct);
        var tickets = await GetAllEventTicketsAsync(eventClient, eventId, ct);
        var ticketById = tickets.ToDictionary(t => t.Id);

        var result = candidates
            .Select(c =>
            {
                ticketById.TryGetValue(c.TicketId, out var ticket);
                var grade = ResolveGradeDefinition(c.CourseName) ?? ResolveGradeDefinition(ticket?.TicketName);

                // Derive current/next/double from the ticket grade using the smart enum
                var resolvedGrade = Grade.FromName(grade);
                var currentGrade = resolvedGrade?.Previous ?? Grade.UnGraded;

                return new GradingEventCandidateDto
                {
                    BookingId = c.BookingId,
                    MemberId = c.CandidateId,
                    TicketId = c.TicketId,
                    TicketName = c.CourseName ?? ticket?.TicketName ?? string.Empty,
                    MemberNumber = c.MemberNumber ?? string.Empty,
                    FirstName = c.FirstName ?? string.Empty,
                    LastName = c.LastName ?? string.Empty,
                    GradeName = grade,
                    BookingDate = c.BookingDate,
                    CurrentGrade = currentGrade.Name,
                    NextGrade = currentGrade.Next?.Name ?? string.Empty,
                    DoubleGrade = currentGrade.Double?.Name ?? string.Empty,
                };
            })
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetMemberDetailsAsync(
        MemberDetailsRequest request,
        IMemberClient memberClient,
        CancellationToken ct)
    {
        if (request.MemberIds is not { Count: > 0 })
        {
            return Results.BadRequest("At least one memberId is required.");
        }

        if (request.MemberIds.Count > 100)
        {
            return Results.BadRequest("Maximum 100 member IDs per request.");
        }

        var memberDetails = await LoadMemberDetailsAsync(request.MemberIds, memberClient, ct);

        var result = memberDetails.Select(detail =>
        {
            var currentGrade = Grade.FromCredentials(detail.Credentials);
            var lastGradingDate = Grade.GetLastGradingDate(detail.Credentials);
            var nextGrade = currentGrade.Next;
            var doubleGrade = currentGrade.Double;

            // Collect all issued grade credential names so the UI can check "already graded"
            var issuedGradeNames = (detail.Credentials ?? [])
                .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)
                    && Grade.IsKnownGrade(c.Name))
                .Select(c => c.Name!)
                .ToList();

            return new MemberDetailResult
            {
                MemberId = detail.Id,
                MemberNumber = detail.MemberId ?? string.Empty,
                FirstName = detail.FirstName ?? string.Empty,
                LastName = detail.LastName ?? string.Empty,
                CurrentGrade = currentGrade.Name,
                LastGradingDate = lastGradingDate,
                NextGrade = nextGrade?.Name ?? string.Empty,
                DoubleGrade = doubleGrade?.Name ?? string.Empty,
                IssuedGradeNames = issuedGradeNames,
            };
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetSingleMemberDetailsAsync(
        Guid memberId,
        IMemberClient memberClient,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Grading");
        var detail = await memberClient.GetMemberAsync(memberId, ct);

        // Diagnostic: log raw credential data from JustGo
        if (detail.Credentials is { } creds)
        {
            foreach (var c in creds.Where(c => Grade.IsKnownGrade(c.Name)))
            {
                logger.LogInformation(
                    "Member {MemberId} credential: Name={Name}, Status={Status}, GrantedDate={GrantedDate}, LastModificationDate={LastModDate}",
                    memberId, c.Name, c.Status, c.GrantedDate, c.LastModificationDate);
            }
        }
        else
        {
            logger.LogWarning("Member {MemberId} has NO credentials array", memberId);
        }

        var currentGrade = Grade.FromCredentials(detail.Credentials);
        var lastGradingDate = Grade.GetLastGradingDate(detail.Credentials);
        logger.LogInformation("Member {MemberId} resolved: Grade={Grade}, LastGradingDate={LastGradingDate}",
            memberId, currentGrade.Name, lastGradingDate);
        var nextGrade = currentGrade.Next;
        var doubleGrade = currentGrade.Double;

        var issuedGradeNames = (detail.Credentials ?? [])
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)
                && Grade.IsKnownGrade(c.Name))
            .Select(c => c.Name!)
            .ToList();

        return Results.Ok(new MemberDetailResult
        {
            MemberId = detail.Id,
            MemberNumber = detail.MemberId ?? string.Empty,
            FirstName = detail.FirstName ?? string.Empty,
            LastName = detail.LastName ?? string.Empty,
            CurrentGrade = currentGrade.Name,
            LastGradingDate = lastGradingDate,
            NextGrade = nextGrade?.Name ?? string.Empty,
            DoubleGrade = doubleGrade?.Name ?? string.Empty,
            IssuedGradeNames = issuedGradeNames,
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

    internal static async Task<IResult> SubmitGradingAsync(
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

        var details = new List<GradingResultStatus>();
        var succeeded = 0;
        var failed = 0;

        foreach (var item in request.Results)
        {
            var gradeName = item.GradeName;
            var bookingId = item.BookingId;
            var bookingCreated = false;

            var validationError = ValidateGradingResult(item);
            if (validationError is not null)
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    BookingId = bookingId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Outcome = item.Outcome,
                    TheoryMark = item.TheoryMark,
                    Success = false,
                    Error = validationError,
                });
                failed++;
                continue;
            }

            if (item.TicketId == Guid.Empty || !ticketById.ContainsKey(item.TicketId))
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Outcome = item.Outcome,
                    TheoryMark = item.TheoryMark,
                    Success = false,
                    Error = "The selected grade ticket was not found for this event.",
                });
                failed++;
                continue;
            }

            MemberDetailDto memberDetail;
            try
            {
                memberDetail = await memberClient.GetMemberAsync(item.MemberId, ct);
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
                    Outcome = item.Outcome,
                    TheoryMark = item.TheoryMark,
                    Success = false,
                    Error = $"Failed to load member details from JustGo before issuing the credential: {GetErrorMessage(ex)}",
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

            var existingCredential = FindIssuedCredentialByName(memberDetail.Credentials, gradeName);
            if (existingCredential is not null)
            {
                details.Add(new GradingResultStatus
                {
                    MemberId = item.MemberId,
                    BookingId = bookingId,
                    TicketId = item.TicketId,
                    MemberNumber = item.MemberNumber,
                    GradeName = gradeName,
                    Outcome = item.Outcome,
                    TheoryMark = item.TheoryMark,
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
                    Outcome = item.Outcome,
                    TheoryMark = item.TheoryMark,
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
                    Outcome = item.Outcome,
                    TheoryMark = item.TheoryMark,
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

    private const int MaxEnrolmentMembersPerRequest = 100;

    internal static async Task<IResult> EnrolMembersAsync(
        Guid eventId,
        GradingEnrolmentRequest request,
        IEventClient eventClient,
        IMemberClient memberClient,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var validationError = ValidateEnrolmentRequest(eventId, request);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var logger = loggerFactory.CreateLogger("JustGo.Api.Features.Grading.GradingEndpoints");
        var tickets = await GetAllEventTicketsAsync(eventClient, eventId, ct);
        var existingCandidates = await GetAllEventCandidatesAsync(eventClient, eventId, ct);
        var bookedMemberIds = existingCandidates.Select(candidate => candidate.CandidateId).ToHashSet();

        var details = new List<GradingEnrolmentStatus>();
        foreach (var item in request.Members)
        {
            var status = bookedMemberIds.Contains(item.MemberId)
                ? CreateAlreadyEnrolledStatus(item, existingCandidates, tickets)
                : await EnrolMemberAsync(eventId, item, tickets, eventClient, memberClient, logger, ct);

            if (status.Outcome is GradingEnrolmentOutcome.Enrolled)
            {
                bookedMemberIds.Add(item.MemberId);
            }

            details.Add(status);
        }

        return Results.Ok(new GradingEnrolmentResponse
        {
            Enrolled = details.Count(detail => detail.Outcome is GradingEnrolmentOutcome.Enrolled),
            AlreadyEnrolled = details.Count(detail => detail.Outcome is GradingEnrolmentOutcome.AlreadyEnrolled),
            Failed = details.Count(detail => detail.Outcome is GradingEnrolmentOutcome.Failed),
            Details = details,
        });
    }

    private static string? ValidateEnrolmentRequest(Guid eventId, GradingEnrolmentRequest request)
    {
        if (eventId == Guid.Empty)
        {
            return "A valid eventId is required.";
        }

        if (request.Members.Count == 0)
        {
            return "At least one member is required.";
        }

        if (request.Members.Count > MaxEnrolmentMembersPerRequest)
        {
            return $"Maximum {MaxEnrolmentMembersPerRequest} members per request.";
        }

        return request.Members.Any(member => member.MemberId == Guid.Empty)
            ? "Every member must have a valid memberId."
            : null;
    }

    private static GradingEnrolmentStatus CreateAlreadyEnrolledStatus(
        GradingEnrolmentItem item,
        IEnumerable<EventCandidateDto> existingCandidates,
        IEnumerable<EventTicketDto> tickets)
    {
        var existing = existingCandidates.FirstOrDefault(candidate => candidate.CandidateId == item.MemberId);
        var ticket = tickets.FirstOrDefault(t => t.Id == existing?.TicketId);

        return new GradingEnrolmentStatus
        {
            MemberId = item.MemberId,
            TicketId = existing?.TicketId,
            BookingId = existing?.BookingId,
            GradeName = ResolveGradeDefinition(existing?.CourseName) ?? ResolveGradeDefinition(ticket?.TicketName),
            Outcome = GradingEnrolmentOutcome.AlreadyEnrolled,
            Error = "Member is already booked onto this grading.",
        };
    }

    private static async Task<GradingEnrolmentStatus> EnrolMemberAsync(
        Guid eventId,
        GradingEnrolmentItem item,
        IReadOnlyList<EventTicketDto> tickets,
        IEventClient eventClient,
        IMemberClient memberClient,
        ILogger logger,
        CancellationToken ct)
    {
        EventTicketDto ticket;
        try
        {
            var resolution = await ResolveEnrolmentTicketAsync(item, tickets, memberClient, ct);
            if (resolution.Error is not null)
            {
                return CreateFailedEnrolmentStatus(item, item.TicketId, gradeName: null, resolution.Error);
            }

            ticket = resolution.Ticket!;
        }
        catch (Exception ex)
        {
            return CreateFailedEnrolmentStatus(
                item,
                item.TicketId,
                gradeName: null,
                $"Failed to load member details from JustGo to choose a grade ticket: {GetErrorMessage(ex)}");
        }

        var gradeName = ResolveGradeDefinition(ticket.TicketName);
        try
        {
            var booking = await eventClient.AddEventCandidateAsync(new EventCandidateCreateRequest
            {
                MemberId = item.MemberId,
                TicketId = ticket.Id,
            }, ct);

            logger.LogInformation(
                "Enrolled member {MemberId} onto grading event {EventId} with ticket {TicketId} (booking {BookingId}).",
                item.MemberId,
                eventId,
                ticket.Id,
                booking.BookingId);

            return new GradingEnrolmentStatus
            {
                MemberId = item.MemberId,
                TicketId = ticket.Id,
                GradeName = gradeName,
                BookingId = booking.BookingId,
                Outcome = GradingEnrolmentOutcome.Enrolled,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enrol member {MemberId} onto grading event {EventId}.", item.MemberId, eventId);
            return CreateFailedEnrolmentStatus(item, ticket.Id, gradeName, GetErrorMessage(ex));
        }
    }

    private static async Task<(EventTicketDto? Ticket, string? Error)> ResolveEnrolmentTicketAsync(
        GradingEnrolmentItem item,
        IReadOnlyList<EventTicketDto> tickets,
        IMemberClient memberClient,
        CancellationToken ct)
    {
        if (item.IsDoubleGrading)
        {
            var memberForDouble = await memberClient.GetMemberAsync(item.MemberId, ct);
            var doubleGrade = Grade.FromCredentials(memberForDouble.Credentials).Double;
            if (doubleGrade is null || !doubleGrade.IsGup)
            {
                return (null, "A double grade must stay within Gup gradings (up to 1st Gup).");
            }

            var doubleTicket = tickets.FirstOrDefault(ticket =>
                string.Equals(ResolveGradeDefinition(ticket.TicketName), doubleGrade.Name, StringComparison.OrdinalIgnoreCase));
            if (doubleTicket is null)
            {
                return (null, $"No ticket for the requested double grade ({doubleGrade.Name}) is available on this grading.");
            }

            if (item.TicketId is { } requestedTicketId && requestedTicketId != Guid.Empty && requestedTicketId != doubleTicket.Id)
            {
                return (null, $"The selected ticket does not match the requested double grade ({doubleGrade.Name}).");
            }

            return (doubleTicket, null);
        }

        if (item.TicketId is { } ticketId && ticketId != Guid.Empty)
        {
            var selected = tickets.FirstOrDefault(ticket => ticket.Id == ticketId);
            return selected is null
                ? (null, "The selected grade ticket was not found for this event.")
                : (selected, null);
        }

        var member = await memberClient.GetMemberAsync(item.MemberId, ct);
        var nextGrade = Grade.FromCredentials(member.Credentials).Next;
        var matching = nextGrade is null
            ? null
            : tickets.FirstOrDefault(ticket =>
                string.Equals(ResolveGradeDefinition(ticket.TicketName), nextGrade.Name, StringComparison.OrdinalIgnoreCase));

        return matching is null
            ? (null, $"No grade ticket matches the member's next grade ({nextGrade?.Name ?? "none"}). Choose a ticket.")
            : (matching, null);
    }

    private static GradingEnrolmentStatus CreateFailedEnrolmentStatus(
        GradingEnrolmentItem item,
        Guid? ticketId,
        string? gradeName,
        string error) =>
        new()
        {
            MemberId = item.MemberId,
            TicketId = ticketId,
            GradeName = gradeName,
            Outcome = GradingEnrolmentOutcome.Failed,
            Error = error,
        };

    private static string? ValidateGradingResult(GradingResultItem item)
    {
        if (!GradingOutcomes.IsValid(item.Outcome))
        {
            return "Outcome must be one of: A, P, or P-.";
        }

        if (item.TheoryMark is < 0 or > 100)
        {
            return "Theory mark must be between 0 and 100 when supplied.";
        }

        return null;
    }

    private static string GetErrorMessage(Exception exception) =>
        exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;

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
        var gradeName = ResolveGradeDefinition(ticket.TicketName);

        return new GradingEventTicketDto
        {
            TicketId = ticket.Id,
            TicketName = ticket.TicketName ?? string.Empty,
            GradeName = gradeName,
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
        var gradeName = ResolveGradeDefinition(candidate.CourseName) ?? ResolveGradeDefinition(ticket?.TicketName);

        memberDetailById.TryGetValue(candidate.CandidateId, out var memberDetail);
        var issuedCredential = gradeName is null
            ? null
            : FindIssuedCredentialByName(memberDetail?.Credentials, gradeName);
        var currentGrade = Grade.FromCredentials(memberDetail?.Credentials);
        var lastGradingDate = Grade.GetLastGradingDate(memberDetail?.Credentials);

        return new GradingEventCandidateDto
        {
            BookingId = candidate.BookingId,
            MemberId = candidate.CandidateId,
            TicketId = candidate.TicketId,
            TicketName = candidate.CourseName ?? ticket?.TicketName ?? string.Empty,
            MemberNumber = candidate.MemberNumber ?? memberDetail?.MemberId ?? string.Empty,
            FirstName = candidate.FirstName ?? memberDetail?.FirstName ?? string.Empty,
            LastName = candidate.LastName ?? memberDetail?.LastName ?? string.Empty,
            GradeName = gradeName,
            BookingDate = candidate.BookingDate,
            HasIssuedCredential = issuedCredential is not null,
            JustGoCredentialId = issuedCredential?.Id,
            CredentialGrantedDate = NormalizeDate(issuedCredential?.GrantedDate),
            CurrentGrade = currentGrade.Name,
            LastGradingDate = lastGradingDate,
            NextGrade = currentGrade.Next?.Name ?? string.Empty,
            DoubleGrade = currentGrade.Double?.Name ?? string.Empty,
        };
    }

    private static string? ResolveGradeDefinition(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Grade.All
            .OrderByDescending(g => g.Name.Length)
            .FirstOrDefault(g => value.Contains(g.Name, StringComparison.OrdinalIgnoreCase))
            ?.Name;
    }

    private static MemberCredentialDtoV2_2? FindIssuedCredentialByName(
        IEnumerable<MemberCredentialDtoV2_2>? credentials,
        string gradeName)
    {
        return credentials?.FirstOrDefault(credential =>
            string.Equals(credential.Name, gradeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(credential.Status, "Active", StringComparison.OrdinalIgnoreCase));
    }

    private static DateOnly? NormalizeDate(DateOnly? value) =>
        value is null || value == DateOnly.MinValue ? null : value;
}
