using Microsoft.Maui.Graphics;

namespace dinospace
{
    // DinoSpace design tokens — warm editorial light theme.
    // One accent (red), warm paper surfaces, black serif headlines, soft
    // shadows instead of hard borders. Inspired by classy recipe-magazine
    // apps rather than "android settings screen".
    public static class Theme
    {
        // Surfaces
        public static readonly Color Bg = Color.FromArgb("#FBF9F5");   // warm paper
        public static readonly Color BgRaised = Color.FromArgb("#FFFFFF");
        public static readonly Color Surface = Color.FromArgb("#FFFFFF");   // cards
        public static readonly Color SurfaceAlt = Color.FromArgb("#F3EFE8");   // subtle wells / tracks
        public static readonly Color SurfaceSunken = Color.FromArgb("#F3EFE8");

        // Lines — thin warm rules like a magazine
        public static readonly Color Hairline = Color.FromArgb("#E7E0D2");
        public static readonly Color HairlineSoft = Color.FromArgb("#EEE9DD");

        // Ink
        public static readonly Color TextPrimary = Color.FromArgb("#1C1B1A");
        public static readonly Color TextSecondary = Color.FromArgb("#6E6963");
        public static readonly Color TextHint = Color.FromArgb("#A39D93");
        public static readonly Color TextOnAccent = Colors.White;

        // One accent. Red. Like the reference.
        public static readonly Color Accent = Color.FromArgb("#D93025");
        public static readonly Color AccentSoft = Color.FromArgb("#FBE9E7");

        // Legacy aliases so every existing view recolours itself without a
        // hundred edits. All three domains now share the single accent.
        public static readonly Color AccentDino = Accent;
        public static readonly Color AccentSpace = Accent;
        public static readonly Color AccentNova = Accent;

        // Feedback
        public static readonly Color Success = Color.FromArgb("#2E7D32");
        public static readonly Color Danger = Color.FromArgb("#C62828");

        // Chips
        public static readonly Color ChipBg = Color.FromArgb("#F3EFE8");
        public static readonly Color ChipText = Color.FromArgb("#57524B");
        public static readonly Color ImgPlaceholder = Color.FromArgb("#EFEAE0");

        public static Color AccentFor(string category) => Accent;

        // The soft card shadow used everywhere instead of borders.
        public static Shadow CardShadow() => new()
        {
            Brush = new SolidColorBrush(Color.FromArgb("#241C1B1A")),
            Offset = new Microsoft.Maui.Graphics.Point(0, 3),
            Radius = 12,
            Opacity = 1f
        };
    }
}
