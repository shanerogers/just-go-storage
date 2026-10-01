namespace JustGo.Grading.Enrolment;

public sealed record GradingEventOption(Guid Id, string Name, DateTime? Date, string? Status);

/// <summary>
/// Orders the bookable grading list: upcoming events soonest first, then any past-dated events
/// JustGo still shows as accepting bookings. Templates, drafts and cancelled events are dropped.
/// </summary>
public static class GradingEventFilter
{
    private static readonly HashSet<string> NeverShownStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Template", "Draft", "Cancelled" };

    private static readonly HashSet<string> FinishedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Complete", "Completed", "Closed" };

    public static bool IsHiddenAlways(GradingEventOption evt) =>
        evt.Status is { } status && NeverShownStatuses.Contains(status.Trim());

    public static bool IsPast(GradingEventOption evt, DateTime today) =>
        (evt.Status is { } status && FinishedStatuses.Contains(status.Trim()))
        || (evt.Date is { } date && date.Date < today.Date);

    /// <summary>
    /// Upcoming gradings soonest first; when <paramref name="includePast"/> is set, past gradings
    /// follow, newest first. <paramref name="keepEventId"/> stays visible so a deep link still resolves.
    /// </summary>
    public static IReadOnlyList<GradingEventOption> SelectVisible(
        IEnumerable<GradingEventOption> events, bool includePast, DateTime today, Guid keepEventId = default)
    {
        var candidates = events.Where(evt => !IsHiddenAlways(evt) || evt.Id == keepEventId).ToList();

        var upcoming = candidates
            .Where(evt => !IsPast(evt, today))
            .OrderBy(evt => evt.Date ?? DateTime.MaxValue);

        var past = candidates
            .Where(evt => IsPast(evt, today) && (includePast || evt.Id == keepEventId))
            .OrderByDescending(evt => evt.Date ?? DateTime.MinValue);

        return [.. upcoming, .. past];
    }
}
