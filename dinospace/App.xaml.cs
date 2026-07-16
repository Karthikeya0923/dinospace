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

            // Warm the face-thumb cache: on a cold start every encyclopedia
            // thumbnail then draws straight from memory instead of popping in
            // one by one as the list decodes them mid-swipe.
            _ = System.Threading.Tasks.Task.Run(Views.FaceArt.WarmAll);

            // If the AI model is on the device, start loading and warming it
            // now, in the background — the cold load takes a while, and doing
            // it at launch means NovaSaur can answer with the full model by
            // the time anyone opens the chat.
            if (Services.NovaSaurService.SupportedPlatform)
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        Services.NovaSaurService.EnsureAutoInit();
                        ModelManager.TryBeginBundledInstall();
                        if (ModelManager.IsModelDownloaded() && !Services.NovaSaurService.IsReady)
                            _ = Services.NovaSaurService.InitAsync();
                    }
                    catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("Model warm start: " + ex); }
                });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Straight into the app after the splash screen.
            return new Window(new AppShell());
        }
    }
}
