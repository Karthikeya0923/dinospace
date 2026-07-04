using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // A full-bleed page background image with a readability scrim on top.
    // If the image file isn't present yet, the transparent layers simply show
    // the page's base colour underneath — so the app looks fine without art.
    public static class Backdrop
    {
        public static View For(string image, double topScrim = 0.35, double bottomScrim = 0.72)
        {
            var img = new Image
            {
                Source = image,
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                InputTransparent = true
            };

            var scrim = new Border
            {
                Background = new LinearGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(Hex(topScrim)), 0f),
                    new GradientStop(Color.FromArgb(Hex(bottomScrim)), 1f),
                }, new Point(0, 0), new Point(0, 1)),
                Stroke = Colors.Transparent,
                InputTransparent = true
            };

            var grid = new Grid { InputTransparent = true };
            grid.Add(img);
            grid.Add(scrim);
            return grid;
        }

        // Builds an ARGB hex like "#59060A12" from a 0..1 alpha over the base bg.
        private static string Hex(double alpha)
        {
            int a = (int)(System.Math.Clamp(alpha, 0, 1) * 255);
            return $"#{a:X2}060A12";
        }
    }
}
