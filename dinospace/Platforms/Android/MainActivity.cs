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
