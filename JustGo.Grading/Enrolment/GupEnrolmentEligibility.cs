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

    public static bool CanRequestDoubleGrade(string? currentGrade, string? doubleGrade) =>
        CanGradeUpToFirstGup(currentGrade)
        && doubleGrade is not null
        && (string.Equals(doubleGrade, "1st Gup", StringComparison.OrdinalIgnoreCase)
            || (!string.Equals(doubleGrade, "UnGraded", StringComparison.OrdinalIgnoreCase)
                && EligibleCurrentGrades.Contains(doubleGrade)));
}
