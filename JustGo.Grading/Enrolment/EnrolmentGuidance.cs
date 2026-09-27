namespace JustGo.Grading.Enrolment;

/// <summary>
/// Wording that walks a club admin through the enrolment steps.
/// </summary>
public static class EnrolmentGuidance
{
    public static string EventPlaceholder(bool isLoading, int visibleCount, int hiddenPastCount = 0) =>
        (isLoading, visibleCount, hiddenPastCount) switch
        {
            (true, _, _) => "Loading gradings from JustGo…",
            (false, 0, > 0) => "No upcoming gradings",
            (false, 0, _) => "No gradings found",
            _ => "Select a grading…"
        };

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
