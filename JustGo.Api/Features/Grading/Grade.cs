using Ardalis.SmartEnum;
using JustGo.Api.Features.Members;

namespace JustGo.Api.Features.Grading;

/// <summary>
/// Smart enum representing ITKD belt grades, ordered from lowest (10th Gup) to highest (9th Dan).
/// Value = rank for ordered navigation (Next/Previous/Double).
/// </summary>
public sealed class Grade : SmartEnum<Grade>
{
    // UnGraded — no credential yet. Rank -1 so .Next = TenthGup.
    public static readonly Grade UnGraded   = new("UnGraded", -1);

    // Gup grades (colour belts) — ranks 0–9
    public static readonly Grade TenthGup   = new("10th Gup", 0);
    public static readonly Grade NinthGup   = new("9th Gup", 1);
    public static readonly Grade EighthGup  = new("8th Gup", 2);
    public static readonly Grade SeventhGup = new("7th Gup", 3);
    public static readonly Grade SixthGup   = new("6th Gup", 4);
    public static readonly Grade FifthGup   = new("5th Gup", 5);
    public static readonly Grade FourthGup  = new("4th Gup", 6);
    public static readonly Grade ThirdGup   = new("3rd Gup", 7);
    public static readonly Grade SecondGup  = new("2nd Gup", 8);
    public static readonly Grade FirstGup   = new("1st Gup", 9);

    // Dan grades (black belts) — ranks 10–18
    public static readonly Grade FirstDan   = new("1st Dan", 10);
    public static readonly Grade SecondDan  = new("2nd Dan", 11);
    public static readonly Grade ThirdDan   = new("3rd Dan", 12);
    public static readonly Grade FourthDan  = new("4th Dan", 13);
    public static readonly Grade FifthDan   = new("5th Dan", 14);
    public static readonly Grade SixthDan   = new("6th Dan", 15);
    public static readonly Grade SeventhDan = new("7th Dan", 16);
    public static readonly Grade EighthDan  = new("8th Dan", 17);
    public static readonly Grade NinthDan   = new("9th Dan", 18);

    private Grade(string name, int value) : base(name, value) { }

    // --- Navigation (uses Value as rank) ---

    /// <summary>The next grade up, or null if already at highest.</summary>
    public Grade? Next => TryFromValue(Value + 1, out var next) ? next : null;

    /// <summary>The previous grade down, or null if already at lowest.</summary>
    public Grade? Previous => TryFromValue(Value - 1, out var prev) ? prev : null;

    /// <summary>Two grades up (for double-grading), or null if not possible.</summary>
    public Grade? Double => TryFromValue(Value + 2, out var dbl) ? dbl : null;

    // --- Convenience properties ---

    /// <summary>The rank (same as Value, for readability).</summary>
    public int Rank => Value;

    /// <summary>True if this is a Gup (colour belt) grade.</summary>
    public bool IsGup => Value >= 0 && Value <= 9;

    /// <summary>True if this is a Dan (black belt) grade.</summary>
    public bool IsDan => Value >= 10;

    // --- Static helpers ---

    /// <summary>All actual grades in rank order (excludes UnGraded).</summary>
    public static IReadOnlyList<Grade> All { get; } = List.Where(g => g != UnGraded).OrderBy(g => g.Value).ToList();

    /// <summary>Find a grade by its credential name (e.g. "5th Dan"). Case-insensitive. Returns null if not found.</summary>
    public static Grade? FromName(string? name) =>
        name is not null && TryFromName(name, ignoreCase: true, out var grade) ? grade : null;

    /// <summary>Returns true if the name matches a known belt grade (excludes UnGraded).</summary>
    public static bool IsKnownGrade(string? name) =>
        name is not null && TryFromName(name, ignoreCase: true, out var grade) && grade != UnGraded;

    /// <summary>
    /// Resolves the highest active grade from a member's credentials.
    /// Returns <see cref="UnGraded"/> if no grade credential is found.
    /// </summary>
    public static Grade FromCredentials(IEnumerable<MemberCredentialDtoV2_2>? credentials)
    {
        if (credentials is null) return UnGraded;

        Grade? highest = null;

        foreach (var cred in credentials)
        {
            if (!string.Equals(cred.Status, "Active", StringComparison.OrdinalIgnoreCase)) continue;
            if (!TryFromName(cred.Name, ignoreCase: true, out var grade)) continue;
            if (grade == UnGraded) continue;
            if (highest is null || grade.Value > highest.Value)
            {
                highest = grade;
            }
        }

        return highest ?? UnGraded;
    }

    /// <summary>
    /// Gets the granted date of the highest active grade credential.
    /// </summary>
    public static DateOnly? GetLastGradingDate(IEnumerable<MemberCredentialDtoV2_2>? credentials)
    {
        if (credentials is null) return null;

        DateOnly? latestDate = null;

        foreach (var cred in credentials)
        {
            if (!string.Equals(cred.Status, "Active", StringComparison.OrdinalIgnoreCase)) continue;
            if (!TryFromName(cred.Name, ignoreCase: true, out var grade)) continue;
            if (grade == UnGraded) continue;

            // Prefer GrantedDate; fall back to LastModificationDate if GrantedDate is missing
            DateOnly? effectiveDate = cred.GrantedDate is { } gd && gd != DateOnly.MinValue
                ? gd
                : cred.LastModificationDate.HasValue
                    ? DateOnly.FromDateTime(cred.LastModificationDate.Value.DateTime)
                    : null;

            if (effectiveDate is not { } date) continue;
            if (latestDate is null || date > latestDate)
            {
                latestDate = date;
            }
        }

        return latestDate;
    }
}
