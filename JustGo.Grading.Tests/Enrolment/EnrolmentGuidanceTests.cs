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
    [InlineData(false, 0, "No gradings are accepting bookings")]
    [InlineData(false, 3, "Select a grading…")]
    public void EventPlaceholder_ReflectsLoadState(bool isLoading, int count, string expected)
    {
        Assert.Equal(expected, EnrolmentGuidance.EventPlaceholder(isLoading, count));
    }

    [Theory]
    [InlineData(0, "Book selected onto grading")]
    [InlineData(1, "Book 1 member onto grading")]
    [InlineData(4, "Book 4 members onto grading")]
    public void BookButtonLabel_IncludesSelectedCount(int count, string expected)
    {
        Assert.Equal(expected, EnrolmentGuidance.BookButtonLabel(count));
    }

    [Theory]
    [InlineData(null, null, EmptyRosterState.Loading)]
    [InlineData(0, null, EmptyRosterState.NoMembers)]
    [InlineData(null, "Failed to load members: timeout", EmptyRosterState.Failed)]
    [InlineData(0, "Failed to load members: timeout", EmptyRosterState.Failed)]
    [InlineData(null, "", EmptyRosterState.Loading)]
    public void DescribeEmptyRoster_DistinguishesLoadingFromEmptyAndFailed(int? total, string? error, EmptyRosterState expected)
    {
        Assert.Equal(expected, EnrolmentGuidance.DescribeEmptyRoster(total, error));
    }

    [Theory]
    [InlineData("Miramar TKD", "There are no members from Miramar TKD to add to the grading.")]
    [InlineData("  ", "There are no members from this club to add to the grading.")]
    [InlineData(null, "There are no members from this club to add to the grading.")]
    public void NoMembersMessage_NamesTheClubWhenKnown(string? club, string expected)
    {
        Assert.Equal(expected, EnrolmentGuidance.NoMembersMessage(club));
    }

    [Fact]
    public void SoleBookableEventId_WithOneEvent_ReturnsItsId()
    {
        var only = new GradingEventOption(Guid.NewGuid(), "Only", DateTime.Today, "Accepting Bookings");

        Assert.Equal(only.Id, EnrolmentGuidance.SoleBookableEventId([only]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void SoleBookableEventId_WithNoneOrSeveral_ReturnsNull(int count)
    {
        var events = Enumerable.Range(0, count)
            .Select(i => new GradingEventOption(Guid.NewGuid(), $"Event {i}", DateTime.Today, "Accepting Bookings"))
            .ToList();

        Assert.Null(EnrolmentGuidance.SoleBookableEventId(events));
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
