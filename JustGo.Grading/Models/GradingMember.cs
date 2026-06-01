namespace JustGo.Grading.Models;

public class GradingMember
{
    public Guid JustGoMemberId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? LastGradingDate { get; set; }
    public string CurrentGrade { get; set; } = string.Empty;
    public string NextGrade { get; set; } = string.Empty;
    public string? DoubleGrade { get; set; }
    public bool Grading { get; set; }
    public bool IsDoubleGrading { get; set; }
    public int? TheoryMark { get; set; }
    public DateTime GradingEventDate { get; set; }
    public bool MatchesEventCredential { get; set; }
    public bool HasIssuedCredential { get; set; }
    public Guid? BookingId { get; set; }
    public bool LoadingGrade { get; set; } = true;
    public bool GradeLoadFailed { get; set; }

    public bool IsWithin90Days =>
        LastGradingDate.HasValue &&
        GradingEventDate > LastGradingDate.Value &&
        (GradingEventDate - LastGradingDate.Value).TotalDays > 90;
}
