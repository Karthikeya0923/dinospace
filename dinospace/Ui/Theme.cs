using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // DinoSpace design tokens. One storybook design language, five wallpaper
    // themes — each one is a complete palette AND a full-screen background
    // drawn behind every page, so text and cards always sit on colours picked
    // for that exact wallpaper (pale ink on the dark skies, deep olive ink on
    // the light papers). Views read Theme.X at build time; switching themes
    // swaps the palette and rebuilds the window.
    public static class Theme
    {
        internal sealed class Palette
        {
            public required Color Bg, BgRaised, Surface, SurfaceAlt, SurfaceSunken;
            public required Color Hairline, HairlineSoft;
            public required Color TextPrimary, TextSecondary, TextHint, TextOnAccent;
            public required Color Accent, AccentSoft;
            public required Color Success, Danger;
            public required Color ChipBg, ChipText, ImgPlaceholder;
            public required Color CardStroke;
            public required float ShadowAlpha;
        }

        // One selectable look: a palette, a wallpaper, and how heavy a wash
        // the wallpaper needs before text sits comfortably on it.
        public sealed class Spec
        {
            public required string Id;
            public required string Name;
            public required string Blurb;
            public required string Wallpaper;  // file in Resources/Images
            public required bool Dark;         // status-bar icon contrast
            public required float Dim;         // readability wash strength
            internal Palette P = null!;
        }

        // The signature look: warm cream paper scattered with little gold
        // stars, sage cards, deep-olive ink. Straight from the design sheet.
        private static readonly Palette StarsP = new()
        {
            Bg = Color.FromArgb("#EEF1E2"),
            BgRaised = Color.FromArgb("#F5F7EA"),
            Surface = Color.FromArgb("#FAFBF1"),
            SurfaceAlt = Color.FromArgb("#E2E9D0"),
            SurfaceSunken = Color.FromArgb("#E6EBD7"),
            Hairline = Color.FromArgb("#B9C4A4"),
            HairlineSoft = Color.FromArgb("#D3DCBF"),
            TextPrimary = Color.FromArgb("#48523C"),
            TextSecondary = Color.FromArgb("#75806A"),
            TextHint = Color.FromArgb("#97A288"),
            TextOnAccent = Color.FromArgb("#FBFDF4"),
            Accent = Color.FromArgb("#6B8A5E"),
            AccentSoft = Color.FromArgb("#DFE8CD"),
            Success = Color.FromArgb("#4C8A4F"),
            Danger = Color.FromArgb("#C05B4D"),
            ChipBg = Color.FromArgb("#E2E9D0"),
            ChipText = Color.FromArgb("#55614A"),
            ImgPlaceholder = Color.FromArgb("#E5EBD6"),
            CardStroke = Color.FromArgb("#C2CCAD"),
            ShadowAlpha = 0.07f
        };

        // The sage graph-paper page: a touch greener, cards go nearly white so
        // they lift off the grid.
        private static readonly Palette GridP = new()
        {
            Bg = Color.FromArgb("#E3EAD5"),
            BgRaised = Color.FromArgb("#EEF3E2"),
            Surface = Color.FromArgb("#FBFCF3"),
            SurfaceAlt = Color.FromArgb("#DEE7CB"),
            SurfaceSunken = Color.FromArgb("#DCE4CA"),
            Hairline = Color.FromArgb("#AEBB97"),
            HairlineSoft = Color.FromArgb("#C9D4B3"),
            TextPrimary = Color.FromArgb("#43503A"),
            TextSecondary = Color.FromArgb("#6F7C63"),
            TextHint = Color.FromArgb("#8FA07E"),
            TextOnAccent = Color.FromArgb("#FBFDF4"),
            Accent = Color.FromArgb("#63855A"),
            AccentSoft = Color.FromArgb("#D9E4C3"),
            Success = Color.FromArgb("#4C8A4F"),
            Danger = Color.FromArgb("#C05B4D"),
            ChipBg = Color.FromArgb("#DEE7CB"),
            ChipText = Color.FromArgb("#505D45"),
            ImgPlaceholder = Color.FromArgb("#E0E8CE"),
            CardStroke = Color.FromArgb("#B7C29E"),
            ShadowAlpha = 0.07f
        };

        // The daydream page: pale cloudy paper, slightly cooler ink.
        private static readonly Palette CloudsP = new()
        {
            Bg = Color.FromArgb("#EAEEE3"),
            BgRaised = Color.FromArgb("#F2F5EB"),
            Surface = Color.FromArgb("#FBFCF6"),
            SurfaceAlt = Color.FromArgb("#E0E7D6"),
            SurfaceSunken = Color.FromArgb("#E2E8D9"),
            Hairline = Color.FromArgb("#B6C1A8"),
            HairlineSoft = Color.FromArgb("#D2DAC5"),
            TextPrimary = Color.FromArgb("#4A5443"),
            TextSecondary = Color.FromArgb("#78826E"),
            TextHint = Color.FromArgb("#99A48C"),
            TextOnAccent = Color.FromArgb("#FBFDF4"),
            Accent = Color.FromArgb("#6E8B63"),
            AccentSoft = Color.FromArgb("#DFE7D2"),
            Success = Color.FromArgb("#4C8A4F"),
            Danger = Color.FromArgb("#C05B4D"),
            ChipBg = Color.FromArgb("#E0E7D6"),
            ChipText = Color.FromArgb("#57614C"),
            ImgPlaceholder = Color.FromArgb("#E3E9DA"),
            CardStroke = Color.FromArgb("#C0CAAF"),
            ShadowAlpha = 0.07f
        };

        // The bedtime page: deep navy night, pale warm ink, starlight-gold
        // accents. Everything that is olive-on-cream by day flips to
        // cream-on-navy so nothing ever sits black-on-black.
        private static readonly Palette NightP = new()
        {
            Bg = Color.FromArgb("#1C2733"),
            BgRaised = Color.FromArgb("#22303F"),
            Surface = Color.FromArgb("#2A3A4C"),
            SurfaceAlt = Color.FromArgb("#35485F"),
            SurfaceSunken = Color.FromArgb("#16202B"),
            Hairline = Color.FromArgb("#47596D"),
            HairlineSoft = Color.FromArgb("#38495B"),
            TextPrimary = Color.FromArgb("#F1F3E7"),
            TextSecondary = Color.FromArgb("#C4CFD8"),
            TextHint = Color.FromArgb("#91A1B0"),
            TextOnAccent = Color.FromArgb("#2A3040"),
            Accent = Color.FromArgb("#E8CD8C"),
            AccentSoft = Color.FromArgb("#3B4757"),
            Success = Color.FromArgb("#9CD08A"),
            Danger = Color.FromArgb("#FF8A76"),
            ChipBg = Color.FromArgb("#35485F"),
            ChipText = Color.FromArgb("#DCE4EB"),
            ImgPlaceholder = Color.FromArgb("#22303F"),
            CardStroke = Color.FromArgb("#47596D"),
            ShadowAlpha = 0f
        };

        // theme5 — the hand-painted twilight artwork a friend made for the
        // app (purple mountains, ember forest, cream planet). Deep plum with
        // a warm cream-gold accent pulled straight from the painting.
        private static readonly Palette DinoP = new()
        {
            Bg = Color.FromArgb("#221338"),
            BgRaised = Color.FromArgb("#2C1B47"),
            Surface = Color.FromArgb("#332052"),
            SurfaceAlt = Color.FromArgb("#422B66"),
            SurfaceSunken = Color.FromArgb("#1A0E2B"),
            Hairline = Color.FromArgb("#4E3775"),
            HairlineSoft = Color.FromArgb("#3F2A61"),
            TextPrimary = Color.FromArgb("#FBF6EB"),
            TextSecondary = Color.FromArgb("#E7DDF7"),
            TextHint = Color.FromArgb("#C3B0E2"),
            TextOnAccent = Color.FromArgb("#33200A"),
            Accent = Color.FromArgb("#EDC46B"),
            AccentSoft = Color.FromArgb("#463217"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF6B5A"),
            ChipBg = Color.FromArgb("#422B66"),
            ChipText = Color.FromArgb("#E7DBF5"),
            ImgPlaceholder = Color.FromArgb("#2C1B47"),
            CardStroke = Color.FromArgb("#4E3775"),
            ShadowAlpha = 0f
        };

        // Every look, in display order. Starry paper is the default.
        public static readonly IReadOnlyList<Spec> Wallpapers = new List<Spec>
        {
            new() { Id = "stars", Name = "starry paper", Blurb = "warm cream paper with little gold stars", Wallpaper = "wall_stars.png", Dark = false, Dim = 0.06f, P = StarsP },
            new() { Id = "grid", Name = "meadow grid", Blurb = "a soft sage graph-paper page", Wallpaper = "wall_grid.png", Dark = false, Dim = 0.28f, P = GridP },
            new() { Id = "clouds", Name = "daydream clouds", Blurb = "pale drifting clouds, calm and quiet", Wallpaper = "wall_clouds.png", Dark = false, Dim = 0.12f, P = CloudsP },
            new() { Id = "night", Name = "night sky", Blurb = "deep navy night full of tiny stars", Wallpaper = "wall_night.png", Dark = true, Dim = 0.22f, P = NightP },
            new() { Id = "dinospace", Name = "dinospace", Blurb = "the hand-painted twilight made for the app", Wallpaper = "theme5.png", Dark = true, Dim = 0.72f, P = DinoP },
        };

        private static Spec _spec = null!;
        public static bool IsDark { get; private set; }
        public static string CurrentId { get; private set; } = "stars";
        public static string? Wallpaper { get; private set; }

        // Applies whatever the user last chose. Ids from older builds map onto
        // the closest current look.
        public static void ApplyCurrent()
        {
            string id = Services.AppSettings.ThemeId switch
            {
                "theme5" or "theme7" => "dinospace",
                "theme2" or "theme3" or "theme4" => "night",
                "stars" or "grid" or "clouds" or "night" or "dinospace" => Services.AppSettings.ThemeId,
                _ => "stars",
            };

            Spec pick = Wallpapers[0];
            foreach (var s in Wallpapers)
                if (s.Id == id) pick = s;

            _spec = pick;
            CurrentId = pick.Id;
            Wallpaper = pick.Wallpaper;
            SetPalette(pick.P, pick.Dark);
        }

        // Mirror the palette into the XAML resource dictionary so Styles.xaml
        // (entries, pages, switches) follows too. Callers rebuild the window.
        private static void SetPalette(Palette p, bool dark)
        {
            IsDark = dark;
            _p = p;

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

        // A wash drawn between the wallpaper and the content so text stays
        // readable no matter how busy the art is.
        public static Color WallpaperDim => Bg.WithAlpha(_spec?.Dim ?? 0.06f);

        private static Palette _p = StarsP;

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

        // Soft card shadow in light themes; dark themes separate cards by tone
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
