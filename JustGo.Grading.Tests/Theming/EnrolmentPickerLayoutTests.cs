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
    public void ClubPicker_DoesNotShowGuidanceBeforeAClubIsSelected()
    {
        var markup = File.ReadAllText(EnrolmentPagePath());

        Assert.Contains("@if (_rosterCache is not null)", markup, StringComparison.Ordinal);
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
