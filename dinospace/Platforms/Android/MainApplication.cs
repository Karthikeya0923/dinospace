using Android.App;
using Android.Runtime;

namespace dinospace
{
    // Android process entry point: the OS constructs this Application once
    // per process and it hands control to the shared MauiProgram. Keep it
    // empty — anything that must run at launch belongs in App/MauiProgram
    // so every platform gets it.
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
