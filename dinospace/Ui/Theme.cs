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

        // Black and gold. Rich, divine. Secondary text is a warm champagne
        // (not grey) so it stays readable and on-theme against near-black.
        private static readonly Palette Dark = new()
        {
            Bg = Color.FromArgb("#0A0908"),
            BgRaised = Color.FromArgb("#131110"),
            Surface = Color.FromArgb("#18150F"),
            SurfaceAlt = Color.FromArgb("#2A2419"),
            SurfaceSunken = Color.FromArgb("#0E0D0A"),
            Hairline = Color.FromArgb("#3D3524"),
            HairlineSoft = Color.FromArgb("#2A2418"),
            TextPrimary = Color.FromArgb("#F8F4EA"),
            TextSecondary = Color.FromArgb("#E7DCC2"),   // bright warm champagne
            TextHint = Color.FromArgb("#C4B896"),        // readable champagne, never grey
            TextOnAccent = Color.FromArgb("#1A1305"),
            Accent = Color.FromArgb("#E3BE55"),          // rich gold
            AccentSoft = Color.FromArgb("#302711"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF5A4D"),          // scary red kept for destructive actions
            ChipBg = Color.FromArgb("#2A2419"),
            ChipText = Color.FromArgb("#E3D8BC"),
            ImgPlaceholder = Color.FromArgb("#1E1A12"),
            CardStroke = Color.FromArgb("#332C1C"),
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
