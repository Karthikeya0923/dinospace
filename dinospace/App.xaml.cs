namespace dinospace
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Whatever look the user last picked: classic light/dark or one of
            // the wallpaper themes.
            Theme.ApplyCurrent();
            UserAppTheme = Theme.IsDark ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Straight into the app after the splash screen.
            return new Window(new AppShell());
        }
    }
}
