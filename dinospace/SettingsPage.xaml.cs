using Microsoft.Maui.ApplicationModel;

namespace dinospace
{
    public partial class SettingsPage : ContentView, ITabView
    {
        // Hosted privacy policy. This exact URL also goes into the
        // Play Console (Policy > App content > Privacy policy).
        private const string PrivacyUrl = "https://karthikeya0923.github.io/dinospace/privacy.html";

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
            try
            {
                await Browser.Default.OpenAsync(PrivacyUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch
            {
                await Shell.Current.DisplayAlert("Privacy Policy",
                    "Couldn't open the browser. You can read the policy at:\n" + PrivacyUrl, "OK");
            }
        }

        // Lets people see how much space the offline AI model uses and
        // delete it to free up ~3 GB. It can be downloaded again anytime.
        private async void OnAiModelTapped(object sender, EventArgs e)
        {
            if (ModelManager.State == DownloadState.Downloading)
            {
                await Shell.Current.DisplayAlert("NovaSaur AI model",
                    "The model is downloading right now. You can pause or stop it from the Ask AI page.", "OK");
                return;
            }

            if (ModelManager.IsModelDownloaded())
            {
                double gb = ModelManager.GetModelSizeBytes() / 1_000_000_000.0;
                bool delete = await Shell.Current.DisplayAlert("NovaSaur AI model",
                    $"NovaSaur is downloaded and uses {gb:0.0} GB of space. Delete it to free up that space? You can download it again anytime from the Ask AI page.",
                    "Delete", "Keep");
                if (!delete) return;

                bool ok = ModelManager.DeleteModel();
                await Shell.Current.DisplayAlert(
                    ok ? "Deleted" : "Couldn't delete",
                    ok ? "The AI model was removed. If NovaSaur was already running, it keeps working until you restart the app."
                       : "Something went wrong deleting the model. Try again after restarting the app.",
                    "OK");
                return;
            }

            if (ModelManager.HasPartialDownload())
            {
                double gb = ModelManager.GetPartialSizeBytes() / 1_000_000_000.0;
                bool delete = await Shell.Current.DisplayAlert("NovaSaur AI model",
                    $"A partly downloaded model ({gb:0.0} GB so far) is on this device. Delete it? The download would start over next time.",
                    "Delete", "Keep");
                if (!delete) return;

                ModelManager.DeleteModel();
                await Shell.Current.DisplayAlert("Deleted", "The partial download was removed.", "OK");
                return;
            }

            await Shell.Current.DisplayAlert("NovaSaur AI model",
                "The model isn't downloaded yet. You can get it from the Ask AI page.", "OK");
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
