using Foundation;

namespace dinospace
{
    // Mac Catalyst bootstrap: same delegate shape as iOS — hand straight
    // over to the shared MauiProgram and let the cross-platform code run.
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
