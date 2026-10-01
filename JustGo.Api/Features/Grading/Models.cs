namespace JustGo.Api.Features.Grading;

public static class GradingOutcomes
{
    public const string A = "A";
    public const string P = "P";
    public const string PMinus = "P-";

    public static bool IsValid(string? outcome) =>
        outcome is A or P or PMinus;
}

/// <summary>Response DTO for a member in the grading context.</summary>
public sealed class GradingMemberDto
{
    public Guid JustGoMemberId { get; init; }
    public string MemberId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string CurrentGrade { get; init; } = string.Empty;
    public DateOnly? LastGradingDate { get; init; }
    public string NextGrade { get; init; } = string.Empty;
    public string DoubleGrade { get; init; } = string.Empty;
}
public sealed class GradingMembersResponse
{
    public List<GradingMemberDto> Members { get; init; } = [];
    public int TotalCount { get; init; }
}

/// <summary>A single member's grading result for submission.</summary>
public sealed class GradingResultItem
{
    public Guid MemberId { get; init; }
    public Guid? BookingId { get; init; }
    public Guid TicketId { get; init; }
    public string MemberNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string GradeName { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public int? TheoryMark { get; init; }
    public bool IsDoubleGrading { get; init; }
}

/// <summary>Batch grading submission request.</summary>
public sealed class GradingSubmitRequest
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateOnly EventDate { get; init; }
    public List<GradingResultItem> Results { get; init; } = [];
}

/// <summary>Status of a single credential issuance.</summary>
public sealed class GradingResultStatus
{
    public Guid MemberId { get; init; }
    public Guid? BookingId { get; init; }
    public Guid TicketId { get; init; }
    public string MemberNumber { get; init; } = string.Empty;
    public string GradeName { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public int? TheoryMark { get; init; }
    public bool Success { get; init; }
    public bool BookingCreated { get; init; }
    public bool SkippedDuplicate { get; init; }
    public string? Error { get; init; }
    public Guid? JustGoCredentialId { get; init; }
}

/// <summary>Batch grading submission response.</summary>
public sealed class GradingSubmitResponse
{
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public List<GradingResultStatus> Details { get; init; } = [];
}

/// <summary>Current JustGo-backed grading state for an event.</summary>
public sealed class GradingEventStateResponse
{
    public Guid EventId { get; init; }
    public List<GradingEventTicketDto> Tickets { get; init; } = [];
    public List<GradingEventCandidateDto> Candidates { get; init; } = [];
}

public sealed class GradingEventTicketDto
{
    public Guid TicketId { get; init; }
    public string TicketName { get; init; } = string.Empty;
    public string? GradeName { get; init; }
    public decimal TotalBooked { get; init; }
    public decimal RemainingPlaces { get; init; }
    public string? TicketCode { get; init; }
    public DateOnly? EndDate { get; init; }
}

public sealed class GradingEventCandidateDto
{
    public Guid BookingId { get; init; }
    public Guid MemberId { get; init; }
    public Guid TicketId { get; init; }
    public string TicketName { get; init; } = string.Empty;
    public string MemberNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? GradeName { get; init; }
    public DateTimeOffset? BookingDate { get; init; }
    public bool HasIssuedCredential { get; init; }
    public Guid? JustGoCredentialId { get; init; }
    public DateOnly? CredentialGrantedDate { get; init; }
    public string CurrentGrade { get; init; } = string.Empty;
    public DateOnly? LastGradingDate { get; init; }
    public string NextGrade { get; init; } = string.Empty;
    public string DoubleGrade { get; init; } = string.Empty;
}

/// <summary>An active club that members can be listed from when enrolling.</summary>
public sealed class GradingClubDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Town { get; init; } = string.Empty;
}

/// <summary>A member to enrol (book) into a grading event.</summary>
public sealed class GradingEnrolmentItem
{
    public Guid MemberId { get; init; }

    /// <summary>Grade ticket to book. When omitted, the ticket matching the member's next grade is used.</summary>
    public Guid? TicketId { get; init; }

    /// <summary>Request the Gup ticket two grades above the member's current grade instead of the next grade.</summary>
    public bool IsDoubleGrading { get; init; }
}

/// <summary>Request body for POST /grading/events/{eventId}/enrolments.</summary>
public sealed class GradingEnrolmentRequest
{
    public List<GradingEnrolmentItem> Members { get; init; } = [];
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<GradingEnrolmentOutcome>))]
public enum GradingEnrolmentOutcome
{
    Enrolled,
    AlreadyEnrolled,
    Failed,
}

/// <summary>Per-member outcome of an enrolment attempt.</summary>
public sealed class GradingEnrolmentStatus
{
    public Guid MemberId { get; init; }
    public Guid? TicketId { get; init; }
    public string? GradeName { get; init; }
    public Guid? BookingId { get; init; }
    public GradingEnrolmentOutcome Outcome { get; init; }
    public string? Error { get; init; }
}

/// <summary>Batch enrolment response summarising each member's outcome.</summary>
public sealed class GradingEnrolmentResponse
{
    public int Enrolled { get; init; }
    public int AlreadyEnrolled { get; init; }
    public int Failed { get; init; }
    public List<GradingEnrolmentStatus> Details { get; init; } = [];
}

/// <summary>Request body for POST /grading/members/details — batch member credential lookup.</summary>
public sealed class MemberDetailsRequest
{
    public List<Guid> MemberIds { get; init; } = [];
}

/// <summary>Grade-resolved member detail returned by POST /grading/members/details.</summary>
public sealed class MemberDetailResult
{
    public Guid MemberId { get; init; }
    public string MemberNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string CurrentGrade { get; init; } = string.Empty;
    public DateOnly? LastGradingDate { get; init; }
    public string NextGrade { get; init; } = string.Empty;
    public string DoubleGrade { get; init; } = string.Empty;
    public List<string> IssuedGradeNames { get; init; } = [];
}
