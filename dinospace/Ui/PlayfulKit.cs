using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // The colour language shared by both layouts: everything keys off the
    // theme accent. The Playful layout used to hand every item its own pastel
    // (powder blue, blush, butter...), but its locked storybook look is a
    // single sage-and-cream page — per-item rainbows fought it, so every
    // accessor now collapses to the accent family in both layouts. The API
    // stays, so call sites didn't have to change.
    public static class PlayfulKit
    {
        // A gently darker partner of the theme accent, for gradients.
        private static Color AccentDeep()
        {
            var a = Theme.Accent;
            return new Color(a.Red * 0.78f, a.Green * 0.78f, a.Blue * 0.78f);
        }

        public static Color HueFor(string key) => Theme.Accent;
        public static Color DeepFor(string key) => AccentDeep();
        public static (Color a, Color b) GradientFor(string key) => (Theme.Accent, AccentDeep());

        // A version of a hue that stays readable as TEXT on a tinted pill: it
        // reads well on dark themes, but sinks into a light theme, so it's
        // deepened there.
        public static Color OnSurface(Color hue)
            => Theme.IsDark ? hue : new Color(hue.Red * 0.62f, hue.Green * 0.62f, hue.Blue * 0.62f);

        public static Color InkFor(string key) => OnSurface(HueFor(key));

        public static (Color a, Color b) Dino => (Theme.Accent, AccentDeep());
        public static (Color a, Color b) Space => (Theme.Accent, AccentDeep());

        public static Color Tab(int index) => Theme.Accent;

        public static Brush Gradient((Color a, Color b) g)
            => new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(g.a, 0), new GradientStop(g.b, 1) },
                new Point(0, 0), new Point(1, 1));

        public static Brush GradientFill(string key) => Gradient(GradientFor(key));
    }
}
