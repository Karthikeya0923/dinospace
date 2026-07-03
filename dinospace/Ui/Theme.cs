using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Code-behind mirror of Resources/Styles/Colors.xaml so views built in C#
    // read the exact same palette as XAML. One source of truth per value.
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb("#070B14");
        public static readonly Color BgRaised = Color.FromArgb("#0C1322");
        public static readonly Color Surface = Color.FromArgb("#131B2E");
        public static readonly Color SurfaceAlt = Color.FromArgb("#1A2440");
        public static readonly Color SurfaceSunken = Color.FromArgb("#0A101D");

        public static readonly Color Hairline = Color.FromArgb("#26314F");
        public static readonly Color HairlineSoft = Color.FromArgb("#1C2740");

        public static readonly Color TextPrimary = Color.FromArgb("#F2F5FC");
        public static readonly Color TextSecondary = Color.FromArgb("#9AA7C4");
        public static readonly Color TextHint = Color.FromArgb("#5F6C8C");
        public static readonly Color TextOnAccent = Color.FromArgb("#0B1020");

        public static readonly Color AccentDino = Color.FromArgb("#FFB74D");
        public static readonly Color AccentSpace = Color.FromArgb("#8C9EFF");
        public static readonly Color AccentNova = Color.FromArgb("#40E0C8");

        public static readonly Color Success = Color.FromArgb("#4ADE80");
        public static readonly Color Danger = Color.FromArgb("#FF7A7A");

        public static readonly Color ChipBg = Color.FromArgb("#223055");
        public static readonly Color ChipText = Color.FromArgb("#C9D4EE");
        public static readonly Color ImgPlaceholder = Color.FromArgb("#182238");

        // The accent that represents a given content domain.
        public static Color AccentFor(string category) => category switch
        {
            "Space" => AccentSpace,
            "Nova" => AccentNova,
            _ => AccentDino,
        };
    }
}
