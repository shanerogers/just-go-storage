namespace JustGo.Api.Features.Grading;

/// <summary>Response DTO for a member in the grading context.</summary>
public sealed class GradingMemberDto
{
    public Guid JustGoMemberId { get; init; }
    public string MemberId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? CurrentGrade { get; init; }
    public Guid? CurrentGradeDefinitionId { get; init; }
    public DateOnly? LastGradingDate { get; init; }
    public string? NextGrade { get; init; }
    public Guid? NextGradeDefinitionId { get; init; }
    public string? DoubleGrade { get; init; }
    public Guid? DoubleGradeDefinitionId { get; init; }
}

/// <summary>Paged response of grading members.</summary>
public sealed class GradingMembersResponse
{
    public List<GradingMemberDto> Members { get; init; } = [];
    public int TotalCount { get; init; }
}

/// <summary>A single member's grading result for submission.</summary>
public sealed class GradingResultItem
{
    public Guid MemberId { get; init; }
    public Guid CredentialDefinitionId { get; init; }
    public string GradeName { get; init; } = string.Empty;
    public int? TheoryMark { get; init; }
    public bool IsDoubleGrading { get; init; }
}

/// <summary>Batch grading submission request.</summary>
public sealed class GradingSubmitRequest
{
    public Guid? EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateOnly EventDate { get; init; }
    public List<GradingResultItem> Results { get; init; } = [];
}

/// <summary>Status of a single credential issuance.</summary>
public sealed class GradingResultStatus
{
    public Guid MemberId { get; init; }
    public string GradeName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Error { get; init; }
}

/// <summary>Batch grading submission response.</summary>
public sealed class GradingSubmitResponse
{
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public List<GradingResultStatus> Details { get; init; } = [];
}
