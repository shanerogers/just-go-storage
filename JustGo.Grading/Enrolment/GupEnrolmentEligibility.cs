namespace JustGo.Grading.Enrolment;

public static class GupEnrolmentEligibility
{
    private static readonly HashSet<string> EligibleCurrentGrades = new(StringComparer.OrdinalIgnoreCase)
    {
        "UnGraded",
        "10th Gup",
        "9th Gup",
        "8th Gup",
        "7th Gup",
        "6th Gup",
        "5th Gup",
        "4th Gup",
        "3rd Gup",
        "2nd Gup",
    };

    public static bool CanGradeUpToFirstGup(string? currentGrade) =>
        currentGrade is not null && EligibleCurrentGrades.Contains(currentGrade);
}
