namespace dinospace
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Show the current app version from the bundle (e.g. "DinoSpace v1.0")
            VersionLabel.Text = $"DinoSpace v{AppInfo.VersionString}";
        }

        // Swipe right -> previous tab (Saved). Settings is the rightmost tab, so no left-swipe.
        private async void OnSwipeRight(object sender, SwipedEventArgs e)
            => await Shell.Current.GoToAsync("//SavedPage");

        private async void OnClearDataTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new ClearDataPage());
        }

        private async void OnCreditsTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new CreditsPage());
        }

        private async void OnWhatsNewTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new WhatsNewPage());
        }

        // Privacy policy placeholder until the Play Store listing is live
        private async void OnPrivacyPolicyTapped(object sender, EventArgs e)
        {
            await DisplayAlertAsync("Coming Soon", "Privacy policy will be available when DinoSpace launches on Google Play.", "OK");
        }

        // Opens the default email app pre-filled with the feedback address
        private async void OnFeedbackTapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync("mailto:dinospace.app@gmail.com?subject=DinoSpace Feedback");
            }
            catch
            {
                await DisplayAlertAsync("No Email App", "No email app was found on this device.", "OK");
            }
        }
    }
}