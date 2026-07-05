using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Platform glue for a seamless theme switch: paints the native window
    // background the target colour so the page swap never flashes white, and
    // tints the system status/navigation bars to match the app so there is no
    // mismatched band above or below the app's own bars.
    public static class ThemeFx
    {
        public static void SetWindowBackground(Color c)
        {
#if ANDROID
            try
            {
                var win = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                if (win == null) return;
                win.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(ToAndroid(c)));
            }
            catch { }
#endif
        }

        // Tint the system bars to the current theme. The navigation bar is
        // painted the same colour as the bottom tab bar so the two read as one
        // continuous bar that reaches the very bottom of the screen — instead
        // of the app's tab bar sitting above a contrasting system bar.
        public static void ApplySystemBars()
        {
#if ANDROID
            try
            {
                var win = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                if (win == null) return;

                win.SetNavigationBarColor(ToAndroid(Theme.BgRaised));
                win.SetStatusBarColor(ToAndroid(Theme.Bg));

                var deco = win.DecorView;
                if (deco != null)
                {
                    var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(win, deco);
                    if (controller != null)
                    {
                        // Dark icons on the light theme, light icons on dark.
                        controller.AppearanceLightStatusBars = !Theme.IsDark;
                        controller.AppearanceLightNavigationBars = !Theme.IsDark;
                    }
                }
            }
            catch { }
#endif
        }

#if ANDROID
        private static Android.Graphics.Color ToAndroid(Color c)
            => Android.Graphics.Color.Argb(255, (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
#endif
    }
}
