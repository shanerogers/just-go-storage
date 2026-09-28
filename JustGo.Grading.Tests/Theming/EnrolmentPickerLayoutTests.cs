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
    public void ClubRoster_AllowsLoadedRowsWhileScanningButLocksBookingUntilFullyLoaded()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Matches("aria-label=\"Loading club members\">\\s*@MemberCardSkeleton\\s*</div>", markup);
        Assert.DoesNotContain("RosterSkeletonCount", markup, StringComparison.Ordinal);
        Assert.Contains("scanToEnd: true", markup, StringComparison.Ordinal);
        Assert.Contains("private bool IsRosterReady => _rosterCache?.IsComplete == true && _rosterCache.Error is null", markup, StringComparison.Ordinal);
        Assert.Contains("row.CanSelect && !_enrolling && !_loadingEvent", markup, StringComparison.Ordinal);
        Assert.Contains("_rosterCache is { Error: null }", markup, StringComparison.Ordinal);
        Assert.Contains("CanInteractWithRow(row) && _rosterCache is { } cache && cache.LoadedItems.Contains(row)", markup, StringComparison.Ordinal);
        Assert.Contains("if (CanChangeRow(row))", markup, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"@(CanInteractWithRow(row) ? \"false\" : \"true\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"@(CanInteractWithRow(row) ? 0 : -1)\"", markup, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(!CanInteractWithRow(row))\"", markup, StringComparison.Ordinal);
        Assert.Contains("OnClick=\"ClearSelection\" Disabled=\"@_enrolling\"", markup, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_selectedRows.Count == 0 || _enrolling || _loadingEvent || !IsRosterReady)\"", markup, StringComparison.Ordinal);
        Assert.Contains("selected.Count == 0 || !IsRosterReady || _loadingEvent || _enrolling", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Booking_DoubleChoiceUsesDoubleGradeTicketAndSendsIntent()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("<MudSwitch T=\"bool\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<MudCheckBox", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"member-card-double\"", markup, StringComparison.Ordinal);
        Assert.Contains("ValueChanged=\"requested => SetDoubleGrade(row, requested)\"", markup, StringComparison.Ordinal);
        Assert.Contains("row.TicketId = FindTicketIdForGrade(requested ? row.DoubleGrade : row.NextGrade);", markup, StringComparison.Ordinal);
        Assert.Contains("row.IsDoubleGrading,", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Grade ticket\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTicket(", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Booking_CardMetadataWrapsSwitchAndMatchesVirtualizedItemHeight()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());
        var styles = File.ReadAllText(Path.Combine(Path.GetDirectoryName(EnrolmentPagePath())!, "..", "..", "wwwroot", "app.css"));

        Assert.Contains("ItemSize=\"120\"", markup, StringComparison.Ordinal);
        Assert.Matches(@"(?s)\.member-card\s*\{[^}]*height: 112px;", styles);
        Assert.Matches(@"(?s)\.member-card-meta\s*\{[^}]*flex-wrap: wrap;", styles);
        Assert.Matches(@"(?s)\.member-card-double \.mud-typography\s*\{[^}]*font-size: 0\.75rem;", styles);
        Assert.Matches(@"(?s)\.member-card-double \.mud-switch-base\.mud-checked \+ \.mud-switch-track\s*\{[^}]*background-color: var\(--retro-orange\);", styles);
        Assert.Matches(@"(?s)\.member-card-double \.mud-switch-base\.mud-checked \.mud-switch-thumb-small\s*\{[^}]*background-color: var\(--retro-orange-ink\);", styles);
        Assert.Contains(".member-card:not(.member-card--skeleton) .member-card-status", styles, StringComparison.Ordinal);
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
