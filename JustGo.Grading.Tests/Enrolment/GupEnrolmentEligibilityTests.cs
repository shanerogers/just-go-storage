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
}
