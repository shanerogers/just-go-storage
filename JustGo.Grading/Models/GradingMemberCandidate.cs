namespace JustGo.Grading.Models;

public sealed class GradingMemberCandidate
{
    public Guid JustGoMemberId { get; init; }
    public string MemberId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime? LastGradingDate { get; init; }
    public string CurrentGrade { get; init; } = string.Empty;
    public string NextGrade { get; init; } = string.Empty;
    public bool IsWithin90Days { get; init; }
}
