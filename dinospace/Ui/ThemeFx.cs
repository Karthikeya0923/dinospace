using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Platform glue for the theme system:
    //  - paints the native window background so page swaps never flash white,
    //  - keeps the status/navigation bars transparent (the app draws edge-to-
    //    edge) with the right light/dark icon contrast,
    //  - and provides a native, screen-covering "freeze frame" used for a
    //    flash-free dark/light cross-dissolve.
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

        // Keep the system bars transparent (content draws behind them) and set
        // the icon contrast for the current theme. The app fills the bar areas
        // itself — the bottom tab bar / input bar via their own inset padding,
        // the top via the window background — so nothing looks mismatched.
        public static void ApplySystemBars()
        {
#if ANDROID
            try
            {
                var win = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                if (win == null) return;

                // Android 15+ enforces transparent bars itself and retires
                // these setters, so they only need calling on older versions.
                if (!System.OperatingSystem.IsAndroidVersionAtLeast(35))
                {
                    win.SetStatusBarColor(Android.Graphics.Color.Transparent);
                    win.SetNavigationBarColor(Android.Graphics.Color.Transparent);
                }

                var deco = win.DecorView;
                if (deco != null)
                {
                    var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(win, deco);
                    if (controller != null)
                    {
                        controller.AppearanceLightStatusBars = !Theme.IsDark;
                        controller.AppearanceLightNavigationBars = !Theme.IsDark;
                    }
                }
            }
            catch { }
#endif
        }

        // Lay a frozen bitmap of the current screen over EVERYTHING at the
        // native level (on the window's decor view, above the system bars) so
        // the app can be fully torn down and rebuilt with the new theme without
        // a single flash showing. Faded away by FadeOutThemeCover once the new
        // UI is on screen.
        public static void ShowThemeCover(byte[] snapshot)
        {
#if ANDROID
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity?.Window?.DecorView is not Android.Views.ViewGroup decor) return;
                RemoveThemeCover();

                var bmp = Android.Graphics.BitmapFactory.DecodeByteArray(snapshot, 0, snapshot.Length);
                if (bmp == null) return;

                var iv = new Android.Widget.ImageView(activity);
                iv.SetImageBitmap(bmp);
                iv.SetScaleType(Android.Widget.ImageView.ScaleType.FitXy);
                iv.LayoutParameters = new Android.Views.ViewGroup.LayoutParams(
                    Android.Views.ViewGroup.LayoutParams.MatchParent,
                    Android.Views.ViewGroup.LayoutParams.MatchParent);
                iv.Clickable = true; // swallow taps during the transition
                decor.AddView(iv);
                _themeCover = iv;
            }
            catch { }
#endif
        }

        public static void FadeOutThemeCover()
        {
#if ANDROID
            var cover = _themeCover;
            if (cover == null) return;
            try
            {
                cover.Animate()!
                     .Alpha(0f)
                     .SetStartDelay(90)   // let the rebuilt UI settle underneath first
                     .SetDuration(300)
                     .WithEndAction(new Java.Lang.Runnable(RemoveThemeCover))!
                     .Start();
            }
            catch { RemoveThemeCover(); }
#endif
        }

#if ANDROID
        private static Android.Views.View? _themeCover;

        private static void RemoveThemeCover()
        {
            var cover = _themeCover;
            _themeCover = null;
            if (cover == null) return;
            try { (cover.Parent as Android.Views.ViewGroup)?.RemoveView(cover); } catch { }
        }

        private static Android.Graphics.Color ToAndroid(Color c)
            => Android.Graphics.Color.Argb(255, (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
#endif
    }
}
