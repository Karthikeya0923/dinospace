namespace dinospace
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Single fixed editorial light theme.
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Straight into the app after the splash screen.
            return new Window(new AppShell());
        }
    }
}
