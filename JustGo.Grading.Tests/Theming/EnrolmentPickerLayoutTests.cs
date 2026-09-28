using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace JustGo.Grading.Tests.Theming;

public partial class EnrolmentPickerLayoutTests
{
    [Fact]
    public void EventAndClubPickers_ShareTheSameFullWidthDenseLayout()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        var pickers = PickerInsideSharedWrapper().Matches(markup)
            .Select(match => match.Groups["body"].Value.Trim())
            .ToList();

        Assert.Equal(2, pickers.Count);
        Assert.Contains(pickers, tag => tag.StartsWith("<MudSelect", StringComparison.Ordinal));
        Assert.Contains(pickers, tag => tag.StartsWith("<MudAutocomplete", StringComparison.Ordinal));
        Assert.All(pickers, tag => Assert.Contains("Margin=\"Margin.Dense\"", tag, StringComparison.Ordinal));
    }

    [Fact]
    public void ClubPicker_DoesNotShowGuidanceBeforeAClubIsSelected()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("@if (_rosterCache is not null)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose your club to list its members", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<MudAlert Severity=\"Severity.Info\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EnrolmentPage_DoesNotShowClubAdminWarning()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.DoesNotContain("Intended for club admins.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<MudAlert Severity=\"Severity.Warning\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangingClubs_ClearsSelectionBeforeLoadingNewRoster()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());
        var handler = markup.Split("private async Task SelectClubAsync(ClubOption? club)", 2)[1]
            .Split("// A new cache instance", 2)[0];

        var unchangedClub = handler.IndexOf("if (club?.Id == _selectedClub?.Id)", StringComparison.Ordinal);
        var clearSelection = handler.IndexOf("ClearSelection();", StringComparison.Ordinal);
        var changeClub = handler.IndexOf("_selectedClub = club;", StringComparison.Ordinal);
        var resetRoster = handler.IndexOf("ResetRoster();", StringComparison.Ordinal);

        Assert.True(unchangedClub >= 0 && unchangedClub < clearSelection
            && clearSelection < changeClub && changeClub < resetRoster);
    }

    [Fact]
    public void ClubRoster_ShowsOneSkeletonAndLocksBookingUntilFullyLoaded()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Matches("aria-label=\"Loading club members\">\\s*@MemberCardSkeleton\\s*</div>", markup);
        Assert.DoesNotContain("RosterSkeletonCount", markup, StringComparison.Ordinal);
        Assert.Contains("scanToEnd: true", markup, StringComparison.Ordinal);
        Assert.Contains("private bool IsRosterReady => _rosterCache?.IsComplete == true && _rosterCache.Error is null", markup, StringComparison.Ordinal);
        Assert.Contains("if (row.CanSelect && IsRosterReady && !_enrolling)", markup, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_selectedRows.Count == 0 || _enrolling || _loadingEvent || !IsRosterReady)\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Booking_DoubleChoiceUsesDoubleGradeTicketAndSendsIntent()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("ValueChanged=\"requested => SetDoubleGrade(row, requested)\"", markup, StringComparison.Ordinal);
        Assert.Contains("row.TicketId = FindTicketIdForGrade(requested ? row.DoubleGrade : row.NextGrade);", markup, StringComparison.Ordinal);
        Assert.Contains("row.IsDoubleGrading,", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Grade ticket\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTicket(", markup, StringComparison.Ordinal);
    }

    private static string EnrolmentPagePath([CallerFilePath] string testFile = "")
    {
        var directory = new FileInfo(testFile).Directory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "JustGo.Grading", "Components")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "JustGo.Grading", "Components", "Pages", "GradingEnrolment.razor");
    }

    [GeneratedRegex(@"<div class=""enrolment-picker"">(?<body>.*?)</div>", RegexOptions.Singleline)]
    private static partial Regex PickerInsideSharedWrapper();
}
