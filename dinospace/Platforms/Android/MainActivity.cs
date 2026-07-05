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
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Paint the window the theme background so nothing flashes white.
            try { ThemeFx.SetWindowBackground(global::dinospace.Theme.Bg); } catch { }
            try { SetupInsets(); } catch { }
        }

        // The app draws edge-to-edge. MAUI still clears the status bar at the
        // top, but we intercept the window insets and hand MAUI a copy with the
        // BOTTOM inset removed, so it no longer leaves an empty strip above the
        // navigation bar. The real bottom inset is reported to the bars that sit
        // at the bottom (the tab bar and the NovaSaur input) so they fill that
        // area themselves and reach the very bottom of the screen.
        private void SetupInsets()
        {
            var window = Window;
            if (window == null) return;
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, false);

            var content = FindViewById(Android.Resource.Id.Content);
            if (content == null) return;
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(content, new BottomInsetListener());
            AndroidX.Core.View.ViewCompat.RequestApplyInsets(content);
        }

        private sealed class BottomInsetListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
        {
            public AndroidX.Core.View.WindowInsetsCompat? OnApplyWindowInsets(Android.Views.View? v, AndroidX.Core.View.WindowInsetsCompat? insets)
            {
                if (v == null || insets == null) return insets;

                var bars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
                if (bars == null) return insets;
                float density = v.Resources?.DisplayMetrics?.Density ?? 2.75f;
                double bottomDip = bars.Bottom / density;

                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try { dinospace.Views.RootPage.SetBottomInset(bottomDip); } catch { }
                    try { dinospace.Views.NovaView.SetBottomInset(bottomDip); } catch { }
                });

                var noBottom = AndroidX.Core.Graphics.Insets.Of(bars.Left, bars.Top, bars.Right, 0);
                var builder = new AndroidX.Core.View.WindowInsetsCompat.Builder(insets)
                    .SetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars(), noBottom);
                return builder?.Build() ?? insets;
            }
        }

        // Finger-tracking tab pager, ViewPager-style, implemented at the
        // activity level (MAUI gesture recognizers never fire over
        // scrollable content). Touches pass through untouched until the
        // gesture is clearly a horizontal drag; then we send the children a
        // CANCEL and drive RootPage's pager directly.
        private float _downX, _downY, _lastX;
        private long _downTime, _lastTime;
        private float _velocity;           // px per ms, smoothed
        private bool _captured, _ignoring;

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (e == null) return base.DispatchTouchEvent(e);
            float density = Resources?.DisplayMetrics?.Density ?? 2.75f;
            var root = dinospace.Views.RootPage.Current;

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    _downX = _lastX = e.RawX;
                    _downY = e.RawY;
                    _downTime = _lastTime = e.EventTime;
                    _velocity = 0;
                    _captured = false;
                    _ignoring = root == null || !root.CanPan();
                    break;

                case MotionEventActions.Move:
                    if (_ignoring) break;
                    float dx = e.RawX - _downX;
                    float dy = e.RawY - _downY;

                    // update velocity (px/ms, smoothed)
                    long dt = e.EventTime - _lastTime;
                    if (dt > 0)
                    {
                        float inst = (e.RawX - _lastX) / dt;
                        _velocity = 0.7f * inst + 0.3f * _velocity;
                    }
                    _lastX = e.RawX;
                    _lastTime = e.EventTime;

                    if (!_captured)
                    {
                        // Capture once clearly horizontal (~18dp) and not a scroll.
                        if (System.Math.Abs(dx) > 18 * density && System.Math.Abs(dx) > System.Math.Abs(dy) * 1.4f)
                        {
                            _captured = true;
                            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => root!.HandlePanStart());

                            // Children get a CANCEL so lists stop scrolling.
                            var cancel = MotionEvent.Obtain(e);
                            cancel!.Action = MotionEventActions.Cancel;
                            base.DispatchTouchEvent(cancel);
                            cancel.Recycle();
                        }
                        else if (System.Math.Abs(dy) > 26 * density)
                        {
                            _ignoring = true; // committed vertical scroll
                        }
                    }

                    if (_captured)
                    {
                        float dxNow = e.RawX - _downX;
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(
                            () => root!.HandlePanMove(dxNow, density));
                        return true; // we own this gesture now
                    }
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    if (_captured)
                    {
                        float dxEnd = e.RawX - _downX;
                        float vel = _velocity;
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(
                            () => root!.HandlePanEnd(dxEnd, vel, density));
                        _captured = false;
                        return true;
                    }
                    break;
            }

            return base.DispatchTouchEvent(e);
        }
    }
}
