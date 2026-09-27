namespace JustGo.Grading.Enrolment;

/// <summary>
/// Text shown on a member card in the enrolment roster.
/// </summary>
public static class MemberCardText
{
    public const string UnknownGrade = "Unknown";

    public static string Initials(string? firstName, string? lastName)
    {
        var initials = $"{FirstLetter(firstName)}{FirstLetter(lastName)}";
        return initials.Length > 0 ? initials : "?";
    }

    public static string FullName(string? firstName, string? lastName)
    {
        var name = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
        return name.Length > 0 ? name : "Unnamed member";
    }

    /// <summary>
    /// "Current → Next" when both grades are known, otherwise whichever part is known.
    /// Returns null while the current grade is still loading.
    /// </summary>
    public static string? GradeProgression(string? currentGrade, string? nextGrade)
    {
        if (currentGrade is null)
        {
            return null;
        }

        var current = string.IsNullOrWhiteSpace(currentGrade) ? UnknownGrade : currentGrade.Trim();
        return string.IsNullOrWhiteSpace(nextGrade) ? current : $"{current} → {nextGrade.Trim()}";
    }

    private static string FirstLetter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : char.ToUpperInvariant(value.Trim()[0]).ToString();
}
