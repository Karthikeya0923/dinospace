using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Platform glue for a seamless theme switch: paints the native window
    // background the target colour so the page swap never flashes white.
    public static class ThemeFx
    {
        public static void SetWindowBackground(Color c)
        {
#if ANDROID
            try
            {
                var act = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                var win = act?.Window;
                if (win == null) return;
                int r = (int)(c.Red * 255), g = (int)(c.Green * 255), b = (int)(c.Blue * 255);
                var androidColor = Android.Graphics.Color.Argb(255, r, g, b);
                win.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(androidColor));
            }
            catch { }
#endif
        }
    }
}
