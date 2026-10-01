using MudBlazor;

namespace JustGo.Grading.Theming;

/// <summary>
/// Retro developer-tool look (monospace type, boxy borders, hard offset shading)
/// with matching light and dark palettes.
/// </summary>
public static class GradingTheme
{
    public const string MonospaceFont = "JetBrains Mono";
    public const string HeadingFont = "Space Grotesk";
    public const string BorderRadius = "2px";
    public const bool StartsInDarkMode = false;

    public const string Navy = "#0F172A";
    public const string OffWhite = "#F8FAFC";
    public const string Orange = "#FDBA74";
    public const string OrangeInk = "#431407";

    private static readonly string[] MonospaceStack = [MonospaceFont, "ui-monospace", "Consolas", "monospace"];
    private static readonly string[] HeadingStack = [HeadingFont, MonospaceFont, "sans-serif"];

    public static MudTheme Create() => new()
    {
        PaletteLight = CreateLightPalette(),
        PaletteDark = CreateDarkPalette(),
        Typography = CreateTypography(),
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = BorderRadius }
    };

    public static PaletteLight CreateLightPalette() => new()
    {
        Primary = Navy,
        PrimaryContrastText = OffWhite,
        Secondary = "#64748B",
        Tertiary = Orange,
        TertiaryContrastText = OrangeInk,
        Background = OffWhite,
        Surface = "#FFFFFF",
        AppbarBackground = Navy,
        AppbarText = "#FFFFFF",
        DrawerBackground = OffWhite,
        DrawerText = Navy,
        TextPrimary = Navy,
        TextSecondary = "#64748B",
        LinesDefault = "#CBD5E1",
        TableLines = "#CBD5E1",
        TableStriped = "#F1F5F9",
        Divider = "#CBD5E1"
    };

    public static PaletteDark CreateDarkPalette() => new()
    {
        Primary = OffWhite,
        PrimaryContrastText = Navy,
        Secondary = "#A1A1AA",
        Tertiary = Orange,
        TertiaryContrastText = OrangeInk,
        Background = Navy,
        Surface = "#09090B",
        AppbarBackground = "#020617",
        AppbarText = "#FFFFFF",
        DrawerBackground = Navy,
        DrawerText = "#E4E4E7",
        TextPrimary = "#FFFFFF",
        TextSecondary = "#A1A1AA",
        LinesDefault = "#27272A",
        TableLines = "#27272A",
        TableStriped = "#111827",
        Divider = "#27272A"
    };

    public static Typography CreateTypography() => new()
    {
        Default = new DefaultTypography { FontFamily = MonospaceStack },
        H1 = new H1Typography { FontFamily = HeadingStack },
        H2 = new H2Typography { FontFamily = HeadingStack },
        H3 = new H3Typography { FontFamily = HeadingStack },
        H4 = new H4Typography { FontFamily = HeadingStack },
        H5 = new H5Typography { FontFamily = HeadingStack },
        H6 = new H6Typography { FontFamily = HeadingStack, FontWeight = "700" },
        Button = new ButtonTypography { FontFamily = MonospaceStack, TextTransform = "none" }
    };
}
