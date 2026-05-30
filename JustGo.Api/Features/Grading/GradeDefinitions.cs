using JustGo.Api.Features.Members;

namespace JustGo.Api.Features.Grading;

/// <summary>
/// Static mapping of ITKD belt grade credential definitions from JustGo.
/// Ordered from lowest (10th Gup) to highest (9th Dan).
/// </summary>
public static class GradeDefinitions
{
    public static readonly IReadOnlyList<GradeDefinition> All =
    [
        new("10th Gup", Guid.Parse("ac90d95c-b09c-41ec-8514-b0b0146a8c4a"), "Gup Grades"),
        new("9th Gup",  Guid.Parse("39a35051-b14c-444c-8d14-84a2fb394409"), "Gup Grades"),
        new("8th Gup",  Guid.Parse("800951c3-1eb7-46b9-8d7e-d96742ec9897"), "Gup Grades"),
        new("7th Gup",  Guid.Parse("e437bd88-7943-4ca4-b8e2-1c40121d4ac9"), "Gup Grades"),
        new("6th Gup",  Guid.Parse("8f12eb81-46ec-4ead-aa07-7bd4e4b04ea8"), "Gup Grades"),
        new("5th Gup",  Guid.Parse("80849763-39fe-4b90-8cb8-9332d129ada8"), "Gup Grades"),
        new("4th Gup",  Guid.Parse("719876c3-2dbe-4120-a16b-02ba4c5fa8ea"), "Gup Grades"),
        new("3rd Gup",  Guid.Parse("e13c7ae9-a9b7-4540-b54f-ea2c95ca5d6b"), "Gup Grades"),
        new("2nd Gup",  Guid.Parse("b98e8647-ac82-48a2-9437-efdd545024ea"), "Gup Grades"),
        new("1st Gup",  Guid.Parse("4e18d3c9-fc99-4f60-b1b0-d4e55813affe"), "Gup Grades"),
        new("1st Dan",  Guid.Parse("ae6b12a1-d794-483b-993a-55ec562bc78c"), "Dan Grades"),
        new("2nd Dan",  Guid.Parse("2da70e9d-4dbf-4f3f-944a-87b26bef2145"), "Dan Grades"),
        new("3rd Dan",  Guid.Parse("fdd065bb-8d39-4522-8e18-cfef75c1bdac"), "Dan Grades"),
        new("4th Dan",  Guid.Parse("efbf2886-69c5-4983-b05d-53b305186715"), "Dan Grades"),
        new("5th Dan",  Guid.Parse("3f4e2451-d8d6-42dc-ba71-94c3920444a2"), "Dan Grades"),
        new("6th Dan",  Guid.Parse("4cf7c686-05c5-42f2-b6ef-edd148d588e4"), "Dan Grades"),
        new("7th Dan",  Guid.Parse("5e4c881d-836c-4d5f-9d5d-067f073b3eb0"), "Dan Grades"),
        new("8th Dan",  Guid.Parse("eeabee08-a9f2-4d0e-b52a-a5cd06255987"), "Dan Grades"),
        new("9th Dan",  Guid.Parse("b9cf1815-2f91-41b3-8f22-44c22949c6dd"), "Dan Grades"),
    ];

    private static readonly Dictionary<Guid, int> IndexByDefinitionId =
        All.Select((g, i) => (g, i)).ToDictionary(x => x.g.DefinitionId, x => x.i);

    private static readonly Dictionary<Guid, GradeDefinition> ByDefinitionId =
        All.ToDictionary(g => g.DefinitionId);

    private static readonly HashSet<Guid> KnownDefinitionIds =
        [.. All.Select(g => g.DefinitionId)];

    /// <summary>Returns the grade name for a definition ID, or null if unknown.</summary>
    public static string? GetGradeName(Guid definitionId) =>
        ByDefinitionId.TryGetValue(definitionId, out var grade) ? grade.Name : null;

    /// <summary>Returns the next grade above the given one, or null if already at 9th Dan.</summary>
    public static GradeDefinition? GetNextGrade(Guid currentDefinitionId)
    {
        if (!IndexByDefinitionId.TryGetValue(currentDefinitionId, out var index)) return null;
        var nextIndex = index + 1;
        return nextIndex < All.Count ? All[nextIndex] : null;
    }

    /// <summary>
    /// Returns the grade two steps above (for double grading), or null if not possible.
    /// </summary>
    public static GradeDefinition? GetDoubleGrade(Guid currentDefinitionId)
    {
        if (!IndexByDefinitionId.TryGetValue(currentDefinitionId, out var index)) return null;
        var doubleIndex = index + 2;
        return doubleIndex < All.Count ? All[doubleIndex] : null;
    }

    /// <summary>Returns true if the definition ID is a known Gup or Dan grade.</summary>
    public static bool IsKnownGrade(Guid definitionId) => KnownDefinitionIds.Contains(definitionId);

    /// <summary>
    /// Finds the highest (most recent / most advanced) active grade credential for a member.
    /// Returns null if the member has no active grade credentials.
    /// </summary>
    public static GradeDefinition? GetCurrentGrade(IEnumerable<MemberCredentialDtoV2_2>? credentials)
    {
        if (credentials is null) return null;

        GradeDefinition? highest = null;
        var highestIndex = -1;

        foreach (var cred in credentials)
        {
            if (!string.Equals(cred.Status, "Active", StringComparison.OrdinalIgnoreCase)) continue;
            if (!IndexByDefinitionId.TryGetValue(cred.DefinitionId, out var index)) continue;
            if (index > highestIndex)
            {
                highestIndex = index;
                highest = ByDefinitionId[cred.DefinitionId];
            }
        }

        return highest;
    }

    /// <summary>
    /// Gets the granted date of the current (highest active) grade credential.
    /// </summary>
    public static DateOnly? GetLastGradingDate(IEnumerable<MemberCredentialDtoV2_2>? credentials)
    {
        if (credentials is null) return null;

        DateOnly? latestDate = null;
        var highestIndex = -1;

        foreach (var cred in credentials)
        {
            if (!string.Equals(cred.Status, "Active", StringComparison.OrdinalIgnoreCase)) continue;
            if (!IndexByDefinitionId.TryGetValue(cred.DefinitionId, out var index)) continue;
            if (index > highestIndex)
            {
                highestIndex = index;
                latestDate = cred.GrantedDate == DateOnly.MinValue ? null : cred.GrantedDate;
            }
        }

        return latestDate;
    }
}

/// <summary>A belt grade credential definition.</summary>
public sealed record GradeDefinition(string Name, Guid DefinitionId, string CredentialType);
