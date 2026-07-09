using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // The colour language of the Playful layout: a storybook set of soft
    // pastels — sage, powder blue, blush, butter — each with a deeper partner
    // for gradients, picked per-item by name so a given dinosaur always keeps
    // the same friendly hue. Gentle, not neon: the look is a picture-book
    // meadow, not a sticker sheet.
    //
    // The kit is also where the two layouts split: in the Native layout every
    // accessor collapses to the theme accent, so grown-up screens stay
    // monochrome and calm without any caller needing to check the mode.
    public static class PlayfulKit
    {
        public static readonly (Color a, Color b)[] Gradients =
        {
            (Color.FromArgb("#8FBC8F"), Color.FromArgb("#5E8C5E")), // sage green
            (Color.FromArgb("#92B8E0"), Color.FromArgb("#5F8BBF")), // powder blue
            (Color.FromArgb("#B5A3DE"), Color.FromArgb("#8971BE")), // soft lavender
            (Color.FromArgb("#E8A8BE"), Color.FromArgb("#C77E97")), // blush pink
            (Color.FromArgb("#E8B98B"), Color.FromArgb("#C28F5C")), // warm peach
            (Color.FromArgb("#8FCBB8"), Color.FromArgb("#5FA48F")), // seafoam mint
            (Color.FromArgb("#E0C98A"), Color.FromArgb("#BA9F58")), // butter yellow
            (Color.FromArgb("#DE9E8F"), Color.FromArgb("#B97263")), // dusty coral
        };

        // Stable index from a name, so the same entry keeps its colour.
        public static int IndexFor(string key)
        {
            int h = 0;
            foreach (char c in key ?? "") h = (h * 31 + c) & 0x7fffffff;
            return h % Gradients.Length;
        }

        // A gently darker partner of the theme accent, for Native gradients.
        private static Color AccentDeep()
        {
            var a = Theme.Accent;
            return new Color(a.Red * 0.78f, a.Green * 0.78f, a.Blue * 0.78f);
        }

        public static Color HueFor(string key)
            => AppLayout.Playful ? Gradients[IndexFor(key)].a : Theme.Accent;
        public static Color DeepFor(string key)
            => AppLayout.Playful ? Gradients[IndexFor(key)].b : AccentDeep();
        public static (Color a, Color b) GradientFor(string key)
            => AppLayout.Playful ? Gradients[IndexFor(key)] : (Theme.Accent, AccentDeep());

        // A version of a hue that stays readable as TEXT on a tinted pill: the
        // pastel reads well on dark themes, but sinks into a light theme, so
        // it's deepened there.
        public static Color OnSurface(Color hue)
            => Theme.IsDark ? hue : new Color(hue.Red * 0.62f, hue.Green * 0.62f, hue.Blue * 0.62f);

        public static Color InkFor(string key) => OnSurface(HueFor(key));

        // Domain feel: dinosaurs sage, space lavender.
        public static (Color a, Color b) Dino
            => AppLayout.Playful ? Gradients[0] : (Theme.Accent, AccentDeep());
        public static (Color a, Color b) Space
            => AppLayout.Playful ? Gradients[2] : (Theme.Accent, AccentDeep());

        // One colour per bottom tab so the Playful nav bar is cheerful and
        // each destination has its own identity (Home, Search, Saved,
        // Settings). Native's flat bar never asks for these.
        public static Color Tab(int index)
        {
            if (!AppLayout.Playful) return Theme.Accent;
            return index switch
            {
                0 => Gradients[0].a,   // Home — sage
                1 => Gradients[1].a,   // Search — powder blue
                2 => Gradients[3].a,   // Saved — blush
                3 => Gradients[2].a,   // Settings — lavender
                _ => Gradients[1].a,
            };
        }

        public static Brush Gradient((Color a, Color b) g)
            => new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(g.a, 0), new GradientStop(g.b, 1) },
                new Point(0, 0), new Point(1, 1));

        public static Brush GradientFill(string key) => Gradient(GradientFor(key));
    }
}
