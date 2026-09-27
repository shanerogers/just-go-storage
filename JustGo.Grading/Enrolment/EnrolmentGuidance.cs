namespace JustGo.Grading.Enrolment;

/// <summary>
/// Wording that walks a club admin through the enrolment steps.
/// </summary>
public static class EnrolmentGuidance
{
    public static string EventPlaceholder(bool isLoading, int visibleCount) =>
        (isLoading, visibleCount) switch
        {
            (true, _) => "Loading gradings from JustGo…",
            (false, 0) => "No gradings are accepting bookings",
            _ => "Select a grading…"
        };

    public static string BookButtonLabel(int selectedCount) =>
        selectedCount switch
        {
            <= 0 => "Book selected onto grading",
            1 => "Book 1 member onto grading",
            _ => $"Book {selectedCount} members onto grading"
        };

    /// <summary>Returns the only bookable grading's id so it can be pre-selected; null when there are none or several.</summary>
    public static Guid? SoleBookableEventId(IReadOnlyList<GradingEventOption> bookableEvents) =>
        bookableEvents.Count == 1 ? bookableEvents[0].Id : null;

    /// <summary>
    /// Explains why the roster has no rows: its first page hasn't arrived, loading failed, or the club has nobody to add.
    /// </summary>
    public static EmptyRosterState DescribeEmptyRoster(int? loadedTotalCount, string? loadError) =>
        (loadedTotalCount, loadError) switch
        {
            (_, { Length: > 0 }) => EmptyRosterState.Failed,
            (null, _) => EmptyRosterState.Loading,
            _ => EmptyRosterState.NoMembers
        };

    public static string NoMembersMessage(string? clubName) =>
        string.IsNullOrWhiteSpace(clubName)
            ? "There are no members from this club to add to the grading."
            : $"There are no members from {clubName.Trim()} to add to the grading.";

    public static string DescribeSelectedEvent(Guid eventId, IReadOnlyDictionary<Guid, string> eventLabels) =>
        eventId != Guid.Empty && eventLabels.TryGetValue(eventId, out var label) ? label : string.Empty;

    public static string FormatEventLabel(string name, DateTime? date) =>
        date is { } value ? $"{name} ({value:dd MMM yyyy})" : name;

    public static string EventPath(Guid eventId) => $"/grading/enrol/{eventId}";

    /// <summary>
    /// True when the browser is not already on the event's enrolment URL. Navigating to the
    /// current URL during prerender turns into a self-redirect loop.
    /// </summary>
    public static bool NeedsNavigationToEvent(Uri currentUri, Guid eventId) =>
        !string.Equals(currentUri.AbsolutePath.TrimEnd('/'), EventPath(eventId), StringComparison.OrdinalIgnoreCase);
}
