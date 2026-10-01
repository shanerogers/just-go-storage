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
    public void ClubPicker_LoadsRosterBeforeEventSelection_ButKeepsSelectionAndBookingDisabled()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("_clubsLoad = LoadClubsInBackgroundAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("await LoadEventsAsync();", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose a grading above to unlock this step.", markup, StringComparison.Ordinal);
        Assert.Contains("MaxItems=\"null\" Disabled=\"@(_loadingClubs || _enrolling)\"", markup, StringComparison.Ordinal);
        Assert.Contains("if (_selectedClub is not null && _rosterCache is not null)", markup, StringComparison.Ordinal);
        Assert.Contains("_rosterCache = _selectedClub is null", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_selectedClub is null || _selectedEventId == Guid.Empty", markup, StringComparison.Ordinal);
        Assert.Contains("_eventBookingsLoaded = true;", markup, StringComparison.Ordinal);
        Assert.Contains("row.CanSelect && !_enrolling", markup, StringComparison.Ordinal);
        Assert.Contains("_selectedEventId != Guid.Empty && _selectedClub is not null && _rosterCache is { Error: null }", markup, StringComparison.Ordinal);
        Assert.Contains("private bool IsRosterReady => _eventBookingsLoaded && _selectedEventId != Guid.Empty", markup, StringComparison.Ordinal);
        Assert.Contains("_selectedEventId == Guid.Empty ? \"Choose grading\"", markup, StringComparison.Ordinal);
        Assert.Matches(@"(?s)if \(_selectedClub is not null && _rosterCache is not null\).*?class=""enrolment-roster"".*?OnClick=""EnrolSelectedAsync"".*?</div>\s*\}\s*</MudPaper>", markup);
    }

    [Fact]
    public void EventDetails_LoadInParallelWithRoster_AndBookedMembersAreDeselectedBeforeBookingUnlocks()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());
        var eventSelection = markup.Split("private async Task SelectEventAsync(Guid eventId)", 2)[1]
            .Split("private async Task LoadEventBookingsAsync()", 2)[0];
        var bookingLoad = markup.Split("private async Task LoadEventBookingsAsync()", 2)[1]
            .Split("private void MarkAlreadyBookedRows()", 2)[0];

        Assert.Contains("@if (_loadingEvents)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@if (_loadingEvents || _loadingEvent)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ResetRoster();", eventSelection, StringComparison.Ordinal);
        Assert.Contains("ClearSelection();", eventSelection, StringComparison.Ordinal);
        Assert.Contains("row.Status = RowStatus.NotBooked;", eventSelection, StringComparison.Ordinal);
        Assert.Contains("row.TicketId = Guid.Empty;", eventSelection, StringComparison.Ordinal);
        Assert.Contains("row.IsDoubleGrading = false;", eventSelection, StringComparison.Ordinal);
        Assert.Contains("await LoadEventBookingsAsync();", eventSelection, StringComparison.Ordinal);
        Assert.True(bookingLoad.IndexOf("MarkAlreadyBookedRows();", StringComparison.Ordinal)
            < bookingLoad.IndexOf("_eventBookingsLoaded = true;", StringComparison.Ordinal));
        Assert.Contains("SetSelected(row, false);", markup.Split("private void MarkAlreadyBookedRows()", 2)[1]
            .Split("private async Task<IEnumerable<ClubOption>>", 2)[0], StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_selectedRows.Count == 0 || _enrolling || _loadingEvent || !IsRosterReady)\"", markup, StringComparison.Ordinal);
        Assert.Contains("_eventBookingsLoaded ? \"Not booked\" : _loadingEvent ? \"Checking bookings\" : \"Booking status unavailable\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ClubPicker_DoesNotShowGuidanceBeforeAClubIsSelected()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("if (_selectedClub is not null && _rosterCache is not null)", markup, StringComparison.Ordinal);
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
        Assert.Contains("private bool IsRosterReady => _eventBookingsLoaded && _selectedEventId != Guid.Empty", markup, StringComparison.Ordinal);
        Assert.Contains("_selectedClub is not null && _rosterCache?.IsComplete == true && _rosterCache.Error is null", markup, StringComparison.Ordinal);
        Assert.Contains("row.CanSelect && !_enrolling", markup, StringComparison.Ordinal);
        Assert.Contains("_rosterCache is { Error: null }", markup, StringComparison.Ordinal);
        Assert.Contains("CanInteractWithRow(row) && _rosterCache is { } cache && cache.LoadedItems.Any(loaded => loaded.MemberId == row.MemberId)", markup, StringComparison.Ordinal);
        Assert.Contains("if (CanChangeRow(row))", markup, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"@(CanInteractWithRow(cardRow) ? \"false\" : \"true\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"@(CanInteractWithRow(cardRow) ? 0 : -1)\"", markup, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(!CanInteractWithRow(cardRow))\"", markup, StringComparison.Ordinal);
        Assert.Contains("OnClick=\"ClearSelection\" Disabled=\"@_enrolling\"", markup, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_selectedRows.Count == 0 || _enrolling || _loadingEvent || !IsRosterReady)\"", markup, StringComparison.Ordinal);
        Assert.Contains("selected.Count == 0 || !IsRosterReady || _loadingEvent || _enrolling", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ClubRoster_ResolvesSelectedCardByMemberIdAfterReordering()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("@key=\"cardRow.MemberId\"", markup, StringComparison.Ordinal);
        Assert.Contains("var cardRow = SelectedRowOrDefault(row);", markup, StringComparison.Ordinal);
        Assert.Contains("_selectedRows.GetValueOrDefault(row.MemberId) ?? row", markup, StringComparison.Ordinal);
        Assert.Contains("cache.LoadedItems.Any(loaded => loaded.MemberId == row.MemberId)", markup, StringComparison.Ordinal);
        Assert.Contains("aria-checked=\"@(cardRow.IsSelected ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("Value=\"cardRow.IsDoubleGrading\"", markup, StringComparison.Ordinal);
        Assert.Contains("ValueChanged=\"requested => SetDoubleGrade(cardRow, requested)\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Booking_DoubleChoiceUsesDoubleGradeTicketAndSendsIntent()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("<MudSwitch T=\"bool\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<MudCheckBox", markup, StringComparison.Ordinal);
        Assert.Contains("@if (cardRow.IsSelected && CanRequestDoubleGrade(cardRow))", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Double grade unavailable", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"member-card-double\"", markup, StringComparison.Ordinal);
        Assert.Contains("ValueChanged=\"requested => SetDoubleGrade(cardRow, requested)\"", markup, StringComparison.Ordinal);
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
