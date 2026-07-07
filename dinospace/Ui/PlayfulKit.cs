using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // The colour language of the Playful layout. The classic layout leans on
    // one theme accent; the kid layout is deliberately rainbow-bright, so this
    // provides a stable set of cheerful hues (each with a deeper partner for
    // gradients) that read clearly on light AND dark themes. Colours are picked
    // per-item by name, so a given dinosaur always gets the same friendly hue.
    public static class PlayfulKit
    {
        public static readonly (Color a, Color b)[] Gradients =
        {
            (Color.FromArgb("#3FBF6A"), Color.FromArgb("#2E8B57")), // grass green
            (Color.FromArgb("#3E9BFF"), Color.FromArgb("#2E5FD0")), // sky blue
            (Color.FromArgb("#A66BFF"), Color.FromArgb("#7C3AED")), // grape purple
            (Color.FromArgb("#FF6FB0"), Color.FromArgb("#E14A97")), // bubblegum pink
            (Color.FromArgb("#FFA23D"), Color.FromArgb("#F2711C")), // orange
            (Color.FromArgb("#25C7C7"), Color.FromArgb("#0E9E9E")), // teal
            (Color.FromArgb("#FF7361"), Color.FromArgb("#E23B3B")), // coral
            (Color.FromArgb("#FFC93C"), Color.FromArgb("#EFA400")), // sunshine
        };

        // Stable index from a name, so the same entry keeps its colour.
        public static int IndexFor(string key)
        {
            int h = 0;
            foreach (char c in key ?? "") h = (h * 31 + c) & 0x7fffffff;
            return h % Gradients.Length;
        }

        public static Color HueFor(string key) => Gradients[IndexFor(key)].a;
        public static Color DeepFor(string key) => Gradients[IndexFor(key)].b;
        public static (Color a, Color b) GradientFor(string key) => Gradients[IndexFor(key)];

        // A version of a hue that stays readable as TEXT on a tinted pill: the
        // bright hue reads well on dark themes, but a bright yellow/orange
        // vanishes on a light theme, so it's darkened there.
        public static Color OnSurface(Color hue)
            => Theme.IsDark ? hue : new Color(hue.Red * 0.72f, hue.Green * 0.72f, hue.Blue * 0.72f);

        public static Color InkFor(string key) => OnSurface(HueFor(key));

        // Domain feel: dinosaurs green, space purple.
        public static (Color a, Color b) Dino => Gradients[0];
        public static (Color a, Color b) Space => Gradients[2];

        // One colour per bottom tab so the nav bar is cheerful and each
        // destination has its own identity (Home, Search, Saved, Settings).
        public static Color Tab(int index) => index switch
        {
            0 => Gradients[0].a,   // Home — green
            1 => Gradients[1].a,   // Search — blue
            2 => Gradients[3].a,   // Saved — pink
            3 => Gradients[2].a,   // Settings — purple
            _ => Gradients[1].a,
        };

        public static Brush Gradient((Color a, Color b) g)
            => new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(g.a, 0), new GradientStop(g.b, 1) },
                new Point(0, 0), new Point(1, 1));

        public static Brush GradientFill(string key) => Gradient(GradientFor(key));
    }
}
