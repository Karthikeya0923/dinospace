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

        // Stickers — bright, cute and cheerful: a white sticker-sheet wallpaper
        // of little green dinos, deep forest-green text, and a fresh leaf-green
        // accent. Built to feel like a kids' sticker book.
        private static readonly Palette StickersP = new()
        {
            Bg = Color.FromArgb("#FFFFFF"),
            BgRaised = Color.FromArgb("#FFFFFF"),
            Surface = Color.FromArgb("#F3FAEF"),
            SurfaceAlt = Color.FromArgb("#E4F4DC"),
            SurfaceSunken = Color.FromArgb("#EEF7E9"),
            Hairline = Color.FromArgb("#CDE8C0"),
            HairlineSoft = Color.FromArgb("#DDF0D4"),
            TextPrimary = Color.FromArgb("#234020"),   // forest green
            TextSecondary = Color.FromArgb("#4F7A46"),
            TextHint = Color.FromArgb("#8FB585"),
            TextOnAccent = Color.FromArgb("#FFFFFF"),
            Accent = Color.FromArgb("#4CAF50"),
            AccentSoft = Color.FromArgb("#DCF1D3"),
            Success = Color.FromArgb("#2E9E4B"),
            Danger = Color.FromArgb("#E5544B"),
            ChipBg = Color.FromArgb("#E4F4DC"),
            ChipText = Color.FromArgb("#356B2E"),
            ImgPlaceholder = Color.FromArgb("#EAF6E4"),
            CardStroke = Colors.Transparent,
            ShadowAlpha = 0.12f
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

        // theme7 — "DinoSpace": the hand-painted twilight artwork made for the
        // app (purple mountains, ember forest, cream planet). Deep plum with a
        // warm cream-gold accent pulled straight from the painting.
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

        // Every look the app offers, in display order. Fossil is the default;
        // theme5 is the app's own painted artwork.
        public static readonly IReadOnlyList<Spec> Wallpapers = new List<Spec>
        {
            new() { Id = "theme1", Name = "Fossil", Blurb = "Warm parchment, light and easy to read", Wallpaper = "theme1.png", Dark = false, P = FossilP },
            new() { Id = "theme2", Name = "Stickers", Blurb = "A cheerful sheet of cute dinos on white", Wallpaper = "stickers.png", Dark = false, P = StickersP },
            new() { Id = "theme3", Name = "Starry Midnight", Blurb = "A calm, star-filled deep blue", Wallpaper = "theme3.png", Dark = true, P = MidnightP },
            new() { Id = "theme4", Name = "Nebula", Blurb = "Soft violet clouds where stars are born", Wallpaper = "theme4.png", Dark = true, P = NebulaP },
            new() { Id = "theme5", Name = "DinoSpace", Blurb = "The app's own painted twilight, dinosaurs and all", Wallpaper = "theme5.png", Dark = true, P = DinoP },
        };

        private static Palette _p = FossilP;
        public static bool IsDark { get; private set; }
        public static string CurrentId { get; private set; } = "theme1";
        public static string? Wallpaper { get; private set; }

        // Applies whatever the user last chose. Ids from older builds (the
        // "classic" pair and the retired ember/aurora slots) map onto the
        // closest current look.
        public static void ApplyCurrent()
        {
            string id = Services.AppSettings.ThemeId;
            id = id switch
            {
                "classic" => Services.AppSettings.DarkMode ? "theme2" : "theme1",
                "theme6" => "theme4",   // old nebula slot
                "theme7" => "theme5",   // old dinospace slot
                _ => id,
            };

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
            CurrentId = "theme1";
            Wallpaper = Wallpapers[0].Wallpaper;
            SetPalette(FossilP, dark: false);
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
        // readable no matter how busy the art is. The painted DinoSpace theme
        // needs a much heavier hand than the quiet generated skies.
        public static Color WallpaperDim => CurrentId switch
        {
            "theme5" => Bg.WithAlpha(0.72f),   // hand-painted art: calm it right down
            "theme4" => Bg.WithAlpha(0.45f),
            "theme3" => Bg.WithAlpha(0.38f),
            "theme2" => Bg.WithAlpha(0.62f),   // stickers: a soft white veil, dinos peek through
            _ => Bg.WithAlpha(0.30f),
        };

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
