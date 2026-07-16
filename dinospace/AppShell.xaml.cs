namespace dinospace
{
    // Code-behind for the single-route Shell. RootPage owns all real
    // navigation (tabs + its own stack), so both overrides simply defer to
    // Shell's defaults — they're kept as seams for logging or intercepting
    // navigation without touching RootPage.
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
        }

        // Android hardware back: Shell pops the modal/nav stack first; when
        // the stack is empty the platform minimises the app, which is the
        // behaviour kids' launchers expect.
        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }
    }
}