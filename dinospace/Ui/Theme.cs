using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // DinoSpace design tokens. Two classic moods (warm paper / black & gold)
    // plus a catalogue of full wallpaper themes — each one is a complete
    // palette AND a subtle full-screen background image that every page draws
    // behind its content. Views read Theme.X at build time; switching themes
    // swaps the palette and rebuilds the window, so everything re-skins in
    // one shot.
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

        // One selectable look: a palette, a mood, and (for wallpaper themes)
        // a background image drawn behind every page.
        public sealed class Spec
        {
            public required string Id;
            public required string Name;
            public required string Blurb;
            public string? Wallpaper;      // file in Resources/Images, null = plain colour
            public required bool Dark;     // status-bar icon contrast
            internal Palette P = null!;    // set by the catalogue below
        }

        private static readonly Palette LightP = new()
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
        private static readonly Palette DarkP = new()
        {
            Bg = Color.FromArgb("#0A0908"),
            BgRaised = Color.FromArgb("#131110"),
            Surface = Color.FromArgb("#18150F"),
            SurfaceAlt = Color.FromArgb("#2A2419"),
            SurfaceSunken = Color.FromArgb("#0E0D0A"),
            Hairline = Color.FromArgb("#3D3524"),
            HairlineSoft = Color.FromArgb("#2A2418"),
            TextPrimary = Color.FromArgb("#F8F4EA"),
            TextSecondary = Color.FromArgb("#E7DCC2"),
            TextHint = Color.FromArgb("#C4B896"),
            TextOnAccent = Color.FromArgb("#1A1305"),
            Accent = Color.FromArgb("#E3BE55"),
            AccentSoft = Color.FromArgb("#302711"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF5A4D"),
            ChipBg = Color.FromArgb("#2A2419"),
            ChipText = Color.FromArgb("#E3D8BC"),
            ImgPlaceholder = Color.FromArgb("#1E1A12"),
            CardStroke = Color.FromArgb("#332C1C"),
            ShadowAlpha = 0f
        };

        // theme6 — built around the hand-painted twilight artwork (deep indigo
        // sky, ember-orange forest light, cream planet). Selectable even before
        // theme6.png is added; the palette alone carries the look.
        private static readonly Palette EmberP = new()
        {
            Bg = Color.FromArgb("#1B1233"),
            BgRaised = Color.FromArgb("#241943"),
            Surface = Color.FromArgb("#2A1E4C"),
            SurfaceAlt = Color.FromArgb("#382861"),
            SurfaceSunken = Color.FromArgb("#150E28"),
            Hairline = Color.FromArgb("#43336E"),
            HairlineSoft = Color.FromArgb("#382A5C"),
            TextPrimary = Color.FromArgb("#F5EDDF"),
            TextSecondary = Color.FromArgb("#D4C5EA"),
            TextHint = Color.FromArgb("#A995C9"),
            TextOnAccent = Color.FromArgb("#2A1405"),
            Accent = Color.FromArgb("#F08A3C"),
            AccentSoft = Color.FromArgb("#452B16"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF6B5A"),
            ChipBg = Color.FromArgb("#382861"),
            ChipText = Color.FromArgb("#E3D8F2"),
            ImgPlaceholder = Color.FromArgb("#241943"),
            CardStroke = Color.FromArgb("#43336E"),
            ShadowAlpha = 0f
        };

        private static readonly Palette MidnightP = new()
        {
            Bg = Color.FromArgb("#070B14"),
            BgRaised = Color.FromArgb("#0E1526"),
            Surface = Color.FromArgb("#111A2E"),
            SurfaceAlt = Color.FromArgb("#1B2742"),
            SurfaceSunken = Color.FromArgb("#050810"),
            Hairline = Color.FromArgb("#263757"),
            HairlineSoft = Color.FromArgb("#1C2A45"),
            TextPrimary = Color.FromArgb("#EDF2FC"),
            TextSecondary = Color.FromArgb("#C0CDE8"),
            TextHint = Color.FromArgb("#8FA1C7"),
            TextOnAccent = Color.FromArgb("#071120"),
            Accent = Color.FromArgb("#7FB4FF"),
            AccentSoft = Color.FromArgb("#14263F"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF6B5A"),
            ChipBg = Color.FromArgb("#1B2742"),
            ChipText = Color.FromArgb("#D5DFF4"),
            ImgPlaceholder = Color.FromArgb("#0E1526"),
            CardStroke = Color.FromArgb("#223250"),
            ShadowAlpha = 0f
        };

        private static readonly Palette AuroraP = new()
        {
            Bg = Color.FromArgb("#05100E"),
            BgRaised = Color.FromArgb("#0B1B18"),
            Surface = Color.FromArgb("#0E211D"),
            SurfaceAlt = Color.FromArgb("#16302A"),
            SurfaceSunken = Color.FromArgb("#030B09"),
            Hairline = Color.FromArgb("#234A40"),
            HairlineSoft = Color.FromArgb("#18332D"),
            TextPrimary = Color.FromArgb("#E9F7F1"),
            TextSecondary = Color.FromArgb("#BFE3D6"),
            TextHint = Color.FromArgb("#85B3A5"),
            TextOnAccent = Color.FromArgb("#04211A"),
            Accent = Color.FromArgb("#4FE0B0"),
            AccentSoft = Color.FromArgb("#103328"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF6B5A"),
            ChipBg = Color.FromArgb("#16302A"),
            ChipText = Color.FromArgb("#CFEDE2"),
            ImgPlaceholder = Color.FromArgb("#0B1B18"),
            CardStroke = Color.FromArgb("#1E4038"),
            ShadowAlpha = 0f
        };

        private static readonly Palette DuskP = new()
        {
            Bg = Color.FromArgb("#1C0F1E"),
            BgRaised = Color.FromArgb("#291731"),
            Surface = Color.FromArgb("#2F1B36"),
            SurfaceAlt = Color.FromArgb("#40254A"),
            SurfaceSunken = Color.FromArgb("#140A16"),
            Hairline = Color.FromArgb("#4E3159"),
            HairlineSoft = Color.FromArgb("#3C2545"),
            TextPrimary = Color.FromArgb("#FBEFE3"),
            TextSecondary = Color.FromArgb("#E8C9C0"),
            TextHint = Color.FromArgb("#BB93A4"),
            TextOnAccent = Color.FromArgb("#2B0E04"),
            Accent = Color.FromArgb("#FF9E6B"),
            AccentSoft = Color.FromArgb("#47231A"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF6B5A"),
            ChipBg = Color.FromArgb("#40254A"),
            ChipText = Color.FromArgb("#F0D8E0"),
            ImgPlaceholder = Color.FromArgb("#291731"),
            CardStroke = Color.FromArgb("#4E3159"),
            ShadowAlpha = 0f
        };

        private static readonly Palette NebulaP = new()
        {
            Bg = Color.FromArgb("#120826"),
            BgRaised = Color.FromArgb("#1C0F38"),
            Surface = Color.FromArgb("#221343"),
            SurfaceAlt = Color.FromArgb("#2F1D57"),
            SurfaceSunken = Color.FromArgb("#0C0519"),
            Hairline = Color.FromArgb("#3D2A6B"),
            HairlineSoft = Color.FromArgb("#2E1F52"),
            TextPrimary = Color.FromArgb("#F3EDFD"),
            TextSecondary = Color.FromArgb("#D6C6F2"),
            TextHint = Color.FromArgb("#A48FD0"),
            TextOnAccent = Color.FromArgb("#1D0B33"),
            Accent = Color.FromArgb("#D98CFF"),
            AccentSoft = Color.FromArgb("#33204F"),
            Success = Color.FromArgb("#8FCB7A"),
            Danger = Color.FromArgb("#FF6B5A"),
            ChipBg = Color.FromArgb("#2F1D57"),
            ChipText = Color.FromArgb("#E5D8F8"),
            ImgPlaceholder = Color.FromArgb("#1C0F38"),
            CardStroke = Color.FromArgb("#3D2A6B"),
            ShadowAlpha = 0f
        };

        private static readonly Palette FossilP = new()
        {
            Bg = Color.FromArgb("#F6EFE2"),
            BgRaised = Color.FromArgb("#FFFDF7"),
            Surface = Color.FromArgb("#FFFDF7"),
            SurfaceAlt = Color.FromArgb("#ECE1CC"),
            SurfaceSunken = Color.FromArgb("#EFE7D6"),
            Hairline = Color.FromArgb("#DCCFB4"),
            HairlineSoft = Color.FromArgb("#E7DCC6"),
            TextPrimary = Color.FromArgb("#2A2118"),
            TextSecondary = Color.FromArgb("#6E5F4B"),
            TextHint = Color.FromArgb("#A08D71"),
            TextOnAccent = Color.FromArgb("#FFF9EC"),
            Accent = Color.FromArgb("#A5652A"),
            AccentSoft = Color.FromArgb("#F4E3D0"),
            Success = Color.FromArgb("#2E7D32"),
            Danger = Color.FromArgb("#C62828"),
            ChipBg = Color.FromArgb("#ECE1CC"),
            ChipText = Color.FromArgb("#5A4A34"),
            ImgPlaceholder = Color.FromArgb("#EDE4D2"),
            CardStroke = Colors.Transparent,
            ShadowAlpha = 0.14f
        };

        // The wallpaper themes shown on the App themes page, in display order.
        // theme6 leads: it's the app's own artwork.
        public static readonly IReadOnlyList<Spec> Wallpapers = new List<Spec>
        {
            new() { Id = "theme6", Name = "Ember Twilight", Blurb = "Dinosaurs under a painted evening sky", Wallpaper = "theme6.png", Dark = true, P = EmberP },
            new() { Id = "theme1", Name = "Starry Midnight", Blurb = "A calm, star-filled deep blue", Wallpaper = "theme1.png", Dark = true, P = MidnightP },
            new() { Id = "theme2", Name = "Aurora", Blurb = "Northern lights over a dark green night", Wallpaper = "theme2.png", Dark = true, P = AuroraP },
            new() { Id = "theme3", Name = "Dusk", Blurb = "That last warm glow after sunset", Wallpaper = "theme3.png", Dark = true, P = DuskP },
            new() { Id = "theme4", Name = "Nebula", Blurb = "Soft violet clouds where stars are born", Wallpaper = "theme4.png", Dark = true, P = NebulaP },
            new() { Id = "theme5", Name = "Fossil", Blurb = "Warm parchment, light and easy to read", Wallpaper = "theme5.png", Dark = false, P = FossilP },
        };

        private static Palette _p = LightP;
        public static bool IsDark { get; private set; }
        public static string CurrentId { get; private set; } = "classic";
        public static string? Wallpaper { get; private set; }

        // Applies whatever the user last chose: a wallpaper theme, or the
        // classic look driven by the dark-mode switch.
        public static void ApplyCurrent()
        {
            string id = Services.AppSettings.ThemeId;
            foreach (var s in Wallpapers)
            {
                if (s.Id == id)
                {
                    CurrentId = s.Id;
                    Wallpaper = s.Wallpaper;
                    SetPalette(s.P, s.Dark);
                    return;
                }
            }
            CurrentId = "classic";
            Wallpaper = null;
            bool dark = Services.AppSettings.DarkMode;
            SetPalette(dark ? DarkP : LightP, dark);
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
