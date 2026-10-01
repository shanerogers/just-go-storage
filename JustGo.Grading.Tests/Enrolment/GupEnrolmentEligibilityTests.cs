using JustGo.Grading.Enrolment;

namespace JustGo.Grading.Tests.Enrolment;

public sealed class GupEnrolmentEligibilityTests
{
    [Theory]
    [InlineData("UnGraded", true)]
    [InlineData("10th Gup", true)]
    [InlineData("5th Gup", true)]
    [InlineData("2nd Gup", true)]
    [InlineData("1st Gup", false)]
    [InlineData("1st Dan", false)]
    [InlineData("9th Dan", false)]
    [InlineData("Unknown", false)]
    [InlineData(null, false)]
    public void CanGradeUpToFirstGup_UsesCurrentGradeName(string? grade, bool expected)
    {
        Assert.Equal(expected, GupEnrolmentEligibility.CanGradeUpToFirstGup(grade));
    }

    [Theory]
    [InlineData("10th Gup", "8th Gup", true)]
    [InlineData("3rd Gup", "1st Gup", true)]
    [InlineData("2nd Gup", "1st Dan", false)]
    [InlineData("1st Gup", "2nd Dan", false)]
    [InlineData("UnGraded", "9th Gup", true)]
    [InlineData("3rd Gup", null, false)]
    public void CanRequestDoubleGrade_StaysWithinGup(string current, string? target, bool expected) =>
        Assert.Equal(expected, GupEnrolmentEligibility.CanRequestDoubleGrade(current, target));
}
