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
            // Straight into the app after the splash screen.
            return new Window(new AppShell());
        }
    }
}
