using JustGo.Grading.Enrolment;

namespace JustGo.Grading.Tests.Enrolment;

public class EnrolmentGuidanceTests
{
    private static readonly Guid KnownEvent = Guid.NewGuid();
    private static readonly Dictionary<Guid, string> Labels = new() { [KnownEvent] = "Kerikeri Grading (14 Dec 2025)" };

    [Fact]
    public void DescribeSelectedEvent_NoSelection_ShowsNothingInsteadOfEmptyGuid()
    {
        Assert.Equal(string.Empty, EnrolmentGuidance.DescribeSelectedEvent(Guid.Empty, Labels));
    }

    [Fact]
    public void DescribeSelectedEvent_KnownEvent_ShowsItsLabel()
    {
        Assert.Equal("Kerikeri Grading (14 Dec 2025)", EnrolmentGuidance.DescribeSelectedEvent(KnownEvent, Labels));
    }

    [Fact]
    public void DescribeSelectedEvent_UnknownEvent_ShowsNothing()
    {
        Assert.Equal(string.Empty, EnrolmentGuidance.DescribeSelectedEvent(Guid.NewGuid(), Labels));
    }

    [Theory]
    [InlineData(true, 0, "Loading gradings from JustGo…")]
    [InlineData(false, 0, "No gradings found")]
    [InlineData(false, 3, "Select a grading…")]
    public void EventPlaceholder_ReflectsLoadState(bool isLoading, int count, string expected)
    {
        Assert.Equal(expected, EnrolmentGuidance.EventPlaceholder(isLoading, count));
    }

    [Fact]
    public void EventPlaceholder_WhenOnlyPastGradingsExist_SaysNoUpcoming()
    {
        Assert.Equal("No upcoming gradings", EnrolmentGuidance.EventPlaceholder(false, 0, hiddenPastCount: 12));
    }

    [Fact]
    public void NeedsNavigationToEvent_AlreadyOnEventUrl_IsFalse()
    {
        var uri = new Uri($"http://localhost/grading/enrol/{KnownEvent}");
        Assert.False(EnrolmentGuidance.NeedsNavigationToEvent(uri, KnownEvent));
    }

    [Fact]
    public void NeedsNavigationToEvent_OnBasePageOrOtherEvent_IsTrue()
    {
        Assert.True(EnrolmentGuidance.NeedsNavigationToEvent(new Uri("http://localhost/grading/enrol"), KnownEvent));
        Assert.True(EnrolmentGuidance.NeedsNavigationToEvent(new Uri($"http://localhost/grading/enrol/{Guid.NewGuid()}"), KnownEvent));
    }

    [Fact]
    public void FormatEventLabel_WithAndWithoutDate()
    {
        Assert.Equal("Grading (14 Dec 2025)", EnrolmentGuidance.FormatEventLabel("Grading", new DateTime(2025, 12, 14)));
        Assert.Equal("Grading", EnrolmentGuidance.FormatEventLabel("Grading", null));
    }
}
