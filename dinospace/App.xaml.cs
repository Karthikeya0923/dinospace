namespace dinospace
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Editorial theme: warm paper + red, or black + gold in dark mode.
            Theme.Apply(AppSettings.DarkMode);
            UserAppTheme = AppSettings.DarkMode ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Straight into the app after the splash screen.
            return new Window(new AppShell());
        }
    }
}
