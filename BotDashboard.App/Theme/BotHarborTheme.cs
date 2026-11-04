using MudBlazor;

namespace BotDashboard.App.Theme;

/// <summary>
/// The Bot Harbor theme. Previously <c>MudThemeProvider</c> was used bare, which meant MudBlazor
/// rendered its default <em>light</em> palette on top of a near-black page background: inputs,
/// menus, dialogs and tables all came out pale against the navy chrome. This defines the dark
/// palette explicitly so the component library and the custom CSS agree on one set of colours.
/// </summary>
public static class BotHarborTheme
{
    /// <summary>Deep navy page background, the darkest surface in the palette.</summary>
    public const string Abyss = "#070B1E";

    /// <summary>Raised card and drawer surface.</summary>
    public const string Hull = "#0D1430";

    /// <summary>Top app bar, a shade between the abyss and the hull.</summary>
    public const string Deck = "#081233";

    /// <summary>Azure accent, used for primary actions.</summary>
    public const string Beacon = "#4C8DFF";

    /// <summary>Violet accent, picked up by the dashboard hero gradient.</summary>
    public const string Dusk = "#7C5CFF";

    /// <summary>Mint accent, used for "start"/"go" affordances and nav icons.</summary>
    public const string Signal = "#22D3A5";

    public static readonly MudTheme Instance = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = Beacon,
            PrimaryContrastText = "#06111F",
            Secondary = Dusk,
            SecondaryContrastText = "#0B0720",
            Tertiary = Signal,
            TertiaryContrastText = "#04231A",

            Info = "#38BDF8",
            Success = "#22C55E",
            Warning = "#FBBF24",
            Error = "#FF4D6D",

            Background = Abyss,
            BackgroundGray = "#050817",
            Surface = Hull,

            AppbarBackground = Deck,
            AppbarText = "#EEF3FF",

            DrawerBackground = Hull,
            DrawerText = "#C7D2F0",
            DrawerIcon = Signal,

            TextPrimary = "#EEF3FF",
            TextSecondary = "#8E9DC4",
            TextDisabled = "#55628A",

            ActionDefault = "#8E9DC4",
            ActionDisabled = "#4A5578",
            ActionDisabledBackground = "#161E3D",

            Divider = "#1E2A52",
            DividerLight = "#161E3D",
            TableLines = "#1E2A52",
            LinesDefault = "#1E2A52",
            LinesInputs = "#2A3760",

            OverlayDark = "rgba(3, 6, 18, 0.72)"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px",
            DrawerWidthLeft = "260px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Montserrat", "Segoe UI", "Helvetica Neue", "sans-serif"],
                LetterSpacing = "0.01em"
            },
            H1 = new H1Typography { FontFamily = ["Montserrat", "sans-serif"], FontWeight = "700" },
            H2 = new H2Typography { FontFamily = ["Montserrat", "sans-serif"], FontWeight = "700" },
            H3 = new H3Typography { FontFamily = ["Montserrat", "sans-serif"], FontWeight = "700" },
            H4 = new H4Typography { FontFamily = ["Montserrat", "sans-serif"], FontWeight = "700" },
            H5 = new H5Typography { FontFamily = ["Montserrat", "sans-serif"], FontWeight = "600" },
            H6 = new H6Typography { FontFamily = ["Montserrat", "sans-serif"], FontWeight = "600" },
            Button = new ButtonTypography
            {
                FontFamily = ["Montserrat", "sans-serif"],
                FontWeight = "600",
                TextTransform = "none",
                LetterSpacing = "0.02em"
            }
        }
    };
}
