using JustGo.Grading.Enrolment;

namespace JustGo.Grading.Tests.Enrolment;

public class GradingEventFilterTests
{
    private static readonly DateTime Today = new(2026, 9, 27);

    private static GradingEventOption Event(string name, int daysFromToday, string? status = "Open") =>
        new(Guid.NewGuid(), name, Today.AddDays(daysFromToday), status);

    [Theory]
    [InlineData("Template")]
    [InlineData("Draft")]
    [InlineData("cancelled")]
    public void SelectVisible_NeverShowsTemplatesDraftsOrCancelled(string status)
    {
        var hidden = Event("Hidden", 5, status);

        var visible = GradingEventFilter.SelectVisible([hidden], includePast: true, Today);

        Assert.Empty(visible);
    }

    [Fact]
    public void SelectVisible_ByDefault_HidesPastAndCompleteGradings()
    {
        var upcoming = Event("Upcoming", 3);
        var pastDated = Event("Past", -3);
        var complete = Event("Complete but future-dated", 10, "Complete");

        var visible = GradingEventFilter.SelectVisible([upcoming, pastDated, complete], includePast: false, Today);

        Assert.Equal([upcoming], visible);
    }

    [Fact]
    public void SelectVisible_OrdersUpcomingSoonestFirst_ThenPastNewestFirst()
    {
        var later = Event("Later", 20);
        var sooner = Event("Sooner", 2);
        var lastWeek = Event("Last week", -7, "Complete");
        var lastYear = Event("Last year", -365, "Complete");

        var visible = GradingEventFilter.SelectVisible([lastYear, later, lastWeek, sooner], includePast: true, Today);

        Assert.Equal([sooner, later, lastWeek, lastYear], visible);
    }

    [Fact]
    public void SelectVisible_TodaysGradingCountsAsUpcoming()
    {
        var today = Event("Today", 0);

        Assert.Equal([today], GradingEventFilter.SelectVisible([today], includePast: false, Today));
    }

    [Fact]
    public void SelectVisible_KeepsDeepLinkedPastEventEvenWhenPastHidden()
    {
        var linked = Event("Linked", -30, "Complete");
        var otherPast = Event("Other", -10, "Complete");

        var visible = GradingEventFilter.SelectVisible([linked, otherPast], includePast: false, Today, linked.Id);

        Assert.Equal([linked], visible);
    }
}
