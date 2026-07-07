namespace dinospace
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Whatever look the user last picked: the theme (colours + wallpaper)
            // and the layout (fonts, shapes, tab bar, home screen).
            AppLayout.ApplyCurrent();
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
