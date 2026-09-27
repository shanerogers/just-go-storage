using JustGo.Grading.Enrolment;

namespace JustGo.Grading.Tests.Enrolment;

public class MemberCardTextTests
{
    [Theory]
    [InlineData("jane", "doe", "JD")]
    [InlineData(" Jane ", "", "J")]
    [InlineData(null, "Doe", "D")]
    [InlineData("", " ", "?")]
    public void Initials_UsesFirstLetterOfEachName(string? first, string? last, string expected) =>
        Assert.Equal(expected, MemberCardText.Initials(first, last));

    [Theory]
    [InlineData("Jane", "Doe", "Jane Doe")]
    [InlineData(" Jane ", null, "Jane")]
    [InlineData(null, null, "Unnamed member")]
    public void FullName_JoinsTrimmedNames(string? first, string? last, string expected) =>
        Assert.Equal(expected, MemberCardText.FullName(first, last));

    [Fact]
    public void GradeProgression_IsNullWhileCurrentGradeIsLoading() =>
        Assert.Null(MemberCardText.GradeProgression(null, "Yellow Belt"));

    [Fact]
    public void GradeProgression_ShowsCurrentToNext() =>
        Assert.Equal("Yellow Stripe → Yellow Belt", MemberCardText.GradeProgression("Yellow Stripe", "Yellow Belt"));

    [Fact]
    public void GradeProgression_ShowsCurrentOnlyWhenNextIsUnknown() =>
        Assert.Equal("Black Belt", MemberCardText.GradeProgression("Black Belt", ""));

    [Fact]
    public void GradeProgression_LabelsBlankCurrentGradeAsUnknown() =>
        Assert.Equal("Unknown → White Stripe", MemberCardText.GradeProgression(" ", "White Stripe"));
}
