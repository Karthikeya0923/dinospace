using dinospace.Views;

namespace dinospace
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Single fixed deep-space theme.
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // First launch shows the 3-slide intro; after that, straight to the app.
            Page root = AppSettings.Onboarded ? new AppShell() : new OnboardingPage();
            return new Window(root);
        }
    }
}
