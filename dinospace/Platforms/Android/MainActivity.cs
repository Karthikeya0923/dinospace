using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace dinospace
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        WindowSoftInputMode = SoftInput.AdjustResize,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Observes every touch (without consuming any) and turns a fast, long,
        // mostly-horizontal fling into a tab switch. This lives at the
        // activity level because MAUI gesture recognizers on the tab host
        // never fire — the scrollable content grabs the touches first.
        private float _downX, _downY;
        private long _downTime;

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (e != null)
            {
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        _downX = e.RawX;
                        _downY = e.RawY;
                        _downTime = e.EventTime;
                        break;

                    case MotionEventActions.Up:
                        float dx = e.RawX - _downX;
                        float dy = e.RawY - _downY;
                        long dt = e.EventTime - _downTime;
                        float width = Resources?.DisplayMetrics?.WidthPixels ?? 1080;

                        // Deliberate horizontal fling: at least 25% of screen
                        // width, clearly more horizontal than vertical, quick.
                        bool isFling = System.Math.Abs(dx) > width * 0.25f
                                       && System.Math.Abs(dx) > System.Math.Abs(dy) * 2.2f
                                       && dt < 450;
                        if (isFling)
                        {
                            int delta = dx < 0 ? +1 : -1; // swipe left = next tab
                            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(
                                () => dinospace.Views.RootPage.Current?.HandleFling(delta));
                        }
                        break;
                }
            }
            return base.DispatchTouchEvent(e);
        }
    }
}
