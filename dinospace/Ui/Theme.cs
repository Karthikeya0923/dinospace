using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // DinoSpace design tokens with two moods:
    //   Light — warm paper, black serif headlines, red accent.
    //   Dark  — near-black, warm white text, rich gold accent.
    // Views read Theme.X at build time; switching modes swaps the palette and
    // rebuilds the window, so everything re-skins in one shot.
    public static class Theme
    {
        private sealed class Palette
        {
            public Color Bg, BgRaised, Surface, SurfaceAlt, SurfaceSunken;
            public Color Hairline, HairlineSoft;
            public Color TextPrimary, TextSecondary, TextHint, TextOnAccent;
            public Color Accent, AccentSoft;
            public Color Success, Danger;
            public Color ChipBg, ChipText, ImgPlaceholder;
            public Color CardStroke;
            public float ShadowAlpha;
        }

        private static readonly Palette Light = new()
        {
            Bg = Color.FromArgb("#FBF9F5"),
            BgRaised = Color.FromArgb("#FFFFFF"),
            Surface = Color.FromArgb("#FFFFFF"),
            SurfaceAlt = Color.FromArgb("#F3EFE8"),
            SurfaceSunken = Color.FromArgb("#F3EFE8"),
            Hairline = Color.FromArgb("#E7E0D2"),
            HairlineSoft = Color.FromArgb("#EEE9DD"),
            TextPrimary = Color.FromArgb("#1C1B1A"),
            TextSecondary = Color.FromArgb("#6E6963"),
            TextHint = Color.FromArgb("#A39D93"),
            TextOnAccent = Colors.White,
            Accent = Color.FromArgb("#D93025"),
            AccentSoft = Color.FromArgb("#FBE9E7"),
            Success = Color.FromArgb("#2E7D32"),
            Danger = Color.FromArgb("#C62828"),
            ChipBg = Color.FromArgb("#F3EFE8"),
            ChipText = Color.FromArgb("#57524B"),
            ImgPlaceholder = Color.FromArgb("#EFEAE0"),
            CardStroke = Colors.Transparent,
            ShadowAlpha = 0.14f
        };

        // Black and gold. Rich, divine.
        private static readonly Palette Dark = new()
        {
            Bg = Color.FromArgb("#0A0908"),
            BgRaised = Color.FromArgb("#12100D"),
            Surface = Color.FromArgb("#161410"),
            SurfaceAlt = Color.FromArgb("#26221A"),
            SurfaceSunken = Color.FromArgb("#0E0D0A"),
            Hairline = Color.FromArgb("#37311F"),
            HairlineSoft = Color.FromArgb("#262114"),
            TextPrimary = Color.FromArgb("#F5F1E6"),
            TextSecondary = Color.FromArgb("#B3AB97"),
            TextHint = Color.FromArgb("#7A7260"),
            TextOnAccent = Color.FromArgb("#171205"),
            Accent = Color.FromArgb("#D9B24A"),
            AccentSoft = Color.FromArgb("#2B2412"),
            Success = Color.FromArgb("#7CB56A"),
            Danger = Color.FromArgb("#E08B7E"),
            ChipBg = Color.FromArgb("#26221A"),
            ChipText = Color.FromArgb("#D8CFB6"),
            ImgPlaceholder = Color.FromArgb("#1C1913"),
            CardStroke = Color.FromArgb("#2C2718"),
            ShadowAlpha = 0f
        };

        private static Palette _p = Light;
        public static bool IsDark { get; private set; }

        // Swap the palette and mirror it into the XAML resource dictionary so
        // Styles.xaml (entries, pages, switches) follows too. Callers rebuild
        // the window afterwards.
        public static void Apply(bool dark)
        {
            IsDark = dark;
            _p = dark ? Dark : Light;

            var res = Application.Current?.Resources;
            if (res == null) return;
            res["Bg"] = _p.Bg;
            res["BgRaised"] = _p.BgRaised;
            res["Surface"] = _p.Surface;
            res["SurfaceAlt"] = _p.SurfaceAlt;
            res["SurfaceSunken"] = _p.SurfaceSunken;
            res["Hairline"] = _p.Hairline;
            res["HairlineSoft"] = _p.HairlineSoft;
            res["TextPrimary"] = _p.TextPrimary;
            res["TextSecondary"] = _p.TextSecondary;
            res["TextHint"] = _p.TextHint;
            res["TextOnAccent"] = _p.TextOnAccent;
            res["Accent"] = _p.Accent;
            res["AccentSoft"] = _p.AccentSoft;
            res["Success"] = _p.Success;
            res["Danger"] = _p.Danger;
            res["ChipBg"] = _p.ChipBg;
            res["ChipText"] = _p.ChipText;
            res["ImgPlaceholder"] = _p.ImgPlaceholder;
        }

        // ---- tokens (live views of the current palette) ----
        public static Color Bg => _p.Bg;
        public static Color BgRaised => _p.BgRaised;
        public static Color Surface => _p.Surface;
        public static Color SurfaceAlt => _p.SurfaceAlt;
        public static Color SurfaceSunken => _p.SurfaceSunken;
        public static Color Hairline => _p.Hairline;
        public static Color HairlineSoft => _p.HairlineSoft;
        public static Color TextPrimary => _p.TextPrimary;
        public static Color TextSecondary => _p.TextSecondary;
        public static Color TextHint => _p.TextHint;
        public static Color TextOnAccent => _p.TextOnAccent;
        public static Color Accent => _p.Accent;
        public static Color AccentSoft => _p.AccentSoft;
        public static Color Success => _p.Success;
        public static Color Danger => _p.Danger;
        public static Color ChipBg => _p.ChipBg;
        public static Color ChipText => _p.ChipText;
        public static Color ImgPlaceholder => _p.ImgPlaceholder;
        public static Color CardStroke => _p.CardStroke;

        // Domain aliases (single accent everywhere).
        public static Color AccentDino => _p.Accent;
        public static Color AccentSpace => _p.Accent;
        public static Color AccentNova => _p.Accent;
        public static Color AccentFor(string category) => _p.Accent;

        // Soft card shadow in light mode; in dark mode cards separate by tone
        // and a faint stroke instead (shadows vanish on black).
        public static Shadow CardShadow() => new()
        {
            Brush = new SolidColorBrush(new Color(0.11f, 0.106f, 0.102f, _p.ShadowAlpha)),
            Offset = new Microsoft.Maui.Graphics.Point(0, 3),
            Radius = 12,
            Opacity = 1f
        };
    }
}
