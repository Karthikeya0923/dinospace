using Microsoft.Maui.ApplicationModel;
using static Google.Android.Material.Tabs.TabLayout;
namespace dinospace
{
    public partial class SettingsPage : ContentView, ITabView
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        public void OnSelected()
        {
            VersionLabel.Text = $"DinoSpace v{AppInfo.VersionString}";
        }

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

        private async void OnPrivacyPolicyTapped(object sender, EventArgs e)
        {
            await Shell.Current.DisplayAlert("Coming Soon", "Privacy policy will be available when DinoSpace launches on Google Play.", "OK");
        }

        private async void OnFeedbackTapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync("mailto:dinospace.app@gmail.com?subject=DinoSpace Feedback");
            }
            catch
            {
                await Shell.Current.DisplayAlert("No Email App", "No email app was found on this device.", "OK");
            }
        }
    }
}