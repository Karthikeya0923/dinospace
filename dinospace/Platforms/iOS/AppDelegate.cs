using Foundation;

namespace dinospace
{
    // iOS bootstrap: UIKit calls into this delegate and it hands control to
    // the shared MauiProgram, so the app behaves identically to Android.
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
