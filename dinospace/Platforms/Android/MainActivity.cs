using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;

namespace dinospace
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private bool _hooked;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // The bottom tab bar isn't created instantly, so we watch the view
            // tree and attach our reselect listener as soon as it appears.
            var decor = Window?.DecorView as ViewGroup;
            if (decor == null) return;

            decor.ViewTreeObserver.AddOnGlobalLayoutListener(
                new GlobalLayoutListener(() =>
                {
                    if (_hooked) return;
                    var bnv = FindBottomNav(decor);
                    if (bnv != null)
                    {
                        _hooked = true;
                        bnv.ItemActiveIndicatorEnabled = false;
                        bnv.SetOnItemReselectedListener(new ReselectListener());
                    }
                }));
        }

        private static BottomNavigationView FindBottomNav(ViewGroup root)
        {
            for (int i = 0; i < root.ChildCount; i++)
            {
                var child = root.GetChildAt(i);
                if (child is BottomNavigationView bnv)
                    return bnv;
                if (child is ViewGroup vg)
                {
                    var found = FindBottomNav(vg);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
        {
            private readonly System.Action _action;
            public GlobalLayoutListener(System.Action action) => _action = action;
            public void OnGlobalLayout() => _action?.Invoke();
        }

        private class ReselectListener : Java.Lang.Object, NavigationBarView.IOnItemReselectedListener
        {
            public void OnNavigationItemReselected(IMenuItem item)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var nav = Shell.Current?.Navigation;
                    if (nav != null && nav.NavigationStack.Count > 1)
                        await nav.PopToRootAsync();
                });
            }
        }
    }
}