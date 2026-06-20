using Microsoft.Extensions.DependencyInjection;

namespace dinospace
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Single fixed theme (frosted glass on dark). Dark-mode toggle removed.
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
