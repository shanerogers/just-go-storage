using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace JustGo.Grading.Tests.Theming;

public partial class LoadingIndicatorColourTests
{
    [Fact]
    public void ProgressIndicators_UseTheSharedOrangeAccent()
    {
        var razorFiles = FindRazorFiles().ToList();
        Assert.NotEmpty(razorFiles);

        var offenders = razorFiles
            .SelectMany(file => ProgressIndicatorTag().Matches(File.ReadAllText(file))
                .Select(match => match.Value)
                .Where(tag => tag.Contains("Color=", StringComparison.Ordinal)
                    && !tag.Contains("Color=\"Color.Tertiary\"", StringComparison.Ordinal))
                .Select(tag => $"{Path.GetFileName(file)}: {tag}"))
            .ToList();

        Assert.Empty(offenders);
    }

    private static IEnumerable<string> FindRazorFiles([CallerFilePath] string testFile = "")
    {
        var directory = new FileInfo(testFile).Directory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "JustGo.Grading", "Components")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Directory.EnumerateFiles(Path.Combine(directory.FullName, "JustGo.Grading", "Components"), "*.razor", SearchOption.AllDirectories);
    }

    [GeneratedRegex(@"<MudProgress(?:Linear|Circular)\b[^>]*>")]
    private static partial Regex ProgressIndicatorTag();
}
