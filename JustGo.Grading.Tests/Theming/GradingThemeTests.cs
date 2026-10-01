using JustGo.Grading.Theming;

namespace JustGo.Grading.Tests.Theming;

public class GradingThemeTests
{
    [Fact]
    public void Create_UsesMonospaceBodyAndGroteskHeadings()
    {
        var theme = GradingTheme.Create();

        Assert.Equal(GradingTheme.MonospaceFont, theme.Typography.Default.FontFamily![0]);
        Assert.Equal(GradingTheme.HeadingFont, theme.Typography.H1.FontFamily![0]);
        Assert.Equal(GradingTheme.HeadingFont, theme.Typography.H6.FontFamily![0]);
    }

    [Fact]
    public void StartsInLightMode_ByDefault()
    {
        Assert.False(GradingTheme.StartsInDarkMode);
    }

    [Fact]
    public void Create_UsesSquareCorners()
    {
        Assert.Equal(GradingTheme.BorderRadius, GradingTheme.Create().LayoutProperties.DefaultBorderRadius);
    }

    [Fact]
    public void LightAndDarkPalettes_InvertNavyAndShareOrangeAccent()
    {
        var light = GradingTheme.CreateLightPalette();
        var dark = GradingTheme.CreateDarkPalette();

        Assert.Equal(GradingTheme.Navy, Hex(light.Primary), ignoreCase: true);
        Assert.Equal(GradingTheme.Navy, Hex(dark.Background), ignoreCase: true);
        Assert.Equal(Hex(light.Tertiary), Hex(dark.Tertiary));
        Assert.Equal(GradingTheme.Orange, Hex(light.Tertiary), ignoreCase: true);
    }

    [Fact]
    public void Secondary_StaysMutedSoCaptionsRemainReadable()
    {
        Assert.Equal(Hex(GradingTheme.CreateLightPalette().TextSecondary), Hex(GradingTheme.CreateLightPalette().Secondary));
        Assert.Equal(Hex(GradingTheme.CreateDarkPalette().TextSecondary), Hex(GradingTheme.CreateDarkPalette().Secondary));
    }

    private static string Hex(MudBlazor.Utilities.MudColor color) =>
        color.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
}
