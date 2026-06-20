namespace dinospace
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private async void OnGitHubTapped(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://github.com/Karthikeya0923/dinospace");
        }

        private async void OnEmailTapped(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("mailto:dinospace.app@gmail.com");
        }
    }
}