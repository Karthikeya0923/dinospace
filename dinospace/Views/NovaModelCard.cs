using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The one place the optional AI model is managed from — shown in the Nova
    // chat (until it's installed) and in Settings (always). Nova answers
    // instantly without the model; installing it upgrades open-ended questions
    // to full streamed answers. Renders every ModelManager state: not
    // downloaded, downloading, paused/failed, assembling Play packs, installed.
    public class NovaModelCard : ContentView
    {
        // The chat hides the card once the model is in; Settings keeps it so
        // the model can be removed to free space.
        private readonly bool _hideWhenInstalled;

        private Label _title = null!;
        private Label _caption = null!;
        private ProgressBar _bar = null!;
        private Border _actionBtn = null!;
        private Label _actionLabel = null!;
        private Label _secondary = null!;

        public NovaModelCard(bool hideWhenInstalled)
        {
            _hideWhenInstalled = hideWhenInstalled;
            Build();
            Refresh();
            // Subscribe only while attached; the Changed event is static and
            // would otherwise keep every discarded card alive.
            HandlerChanged += (_, _) =>
            {
                ModelManager.Changed -= OnChanged;
                if (Handler != null) { ModelManager.Changed += OnChanged; Refresh(); }
            };
        }

        private void OnChanged() => MainThread.BeginInvokeOnMainThread(Refresh);

        private void Build()
        {
            _title = new Label { FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary };
            _caption = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.35, TextColor = Theme.TextSecondary };
            _bar = new ProgressBar { ProgressColor = Theme.AccentNova, IsVisible = false };

            _actionLabel = new Label
            {
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            _actionBtn = new Border
            {
                Content = _actionLabel, BackgroundColor = Theme.AccentNova,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(18, 10), HorizontalOptions = LayoutOptions.Start
            };
            Ui.OnTap(_actionBtn, async (_, _) => await OnAction());

            _secondary = new Label
            {
                FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextSecondary, VerticalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(14, 10), IsVisible = false
            };
            Ui.OnTap(_secondary, async (_, _) => await OnSecondary());

            var buttons = new HorizontalStackLayout { Spacing = 6, Children = { _actionBtn, _secondary } };
            var col = new VerticalStackLayout { Spacing = 8, Children = { _title, _caption, _bar, buttons } };

            Content = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16, 14),
                Shadow = Theme.CardShadow()
            };
        }

        private void Refresh()
        {
            bool installed = ModelManager.IsModelDownloaded();
            if (!NovaSaurService.SupportedPlatform || (installed && _hideWhenInstalled))
            {
                IsVisible = false;
                return;
            }
            IsVisible = true;

            var state = ModelManager.State;
            if (installed)
            {
                _title.Text = "Full AI brain — installed";
                _caption.Text = $"Open-ended questions stream from the on-device model. Takes {Gb(ModelManager.GetModelSizeBytes())} of storage.";
                _bar.IsVisible = false;
                _actionLabel.Text = "Remove to free space";
                _actionBtn.BackgroundColor = Theme.ChipBg;
                _actionLabel.TextColor = Theme.Danger;
                _secondary.IsVisible = false;
                return;
            }

            _actionBtn.BackgroundColor = Theme.AccentNova;
            _actionLabel.TextColor = Theme.TextOnAccent;

            if (state == DownloadState.Downloading && ModelManager.IsLocalInstall)
            {
                _title.Text = "Setting up Nova…";
                _caption.Text = "Joining the model files Google Play delivered — about a minute.";
                _bar.IsVisible = true;
                _bar.Progress = ModelManager.Progress;
                _actionBtn.IsVisible = false;
                _secondary.IsVisible = false;
                return;
            }
            _actionBtn.IsVisible = true;

            if (state == DownloadState.Downloading)
            {
                _title.Text = "Downloading the full brain…";
                _caption.Text = $"{Gb(ModelManager.DoneBytes)} of {Gb(ModelManager.TotalBytes)} — you can keep using the app.";
                _bar.IsVisible = true;
                _bar.Progress = ModelManager.Progress;
                _actionLabel.Text = "Pause";
                _secondary.IsVisible = false;
                return;
            }

            if (ModelManager.HasPartialDownload())
            {
                _title.Text = state == DownloadState.Failed ? "Download hit a snag" : "Download paused";
                _caption.Text = $"{Gb(ModelManager.GetPartialSizeBytes())} saved so far — it resumes right where it stopped.";
                _bar.IsVisible = false;
                _actionLabel.Text = "Resume";
                _secondary.Text = "Delete";
                _secondary.IsVisible = true;
                return;
            }

            _title.Text = "Give Nova its full brain";
            _caption.Text = "Nova already answers instantly from its encyclopedia. Add the on-device AI model (2.4 GB, Wi-Fi recommended) and open-ended questions get full streamed answers — still completely offline.";
            _bar.IsVisible = false;
            _actionLabel.Text = "Download — 2.4 GB";
            _secondary.IsVisible = false;
        }

        private async System.Threading.Tasks.Task OnAction()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (ModelManager.IsModelDownloaded())
            {
                if (page == null) return;
                bool sure = await page.DisplayAlertAsync("Remove the AI model?",
                    "Nova keeps answering encyclopedia questions instantly. You can download the model again any time.",
                    "Remove", "Keep it");
                if (sure) { ModelManager.DeleteModel(); Refresh(); }
                return;
            }

            if (ModelManager.State == DownloadState.Downloading)
            {
                ModelManager.Pause();
                return;
            }

            // Starting (or resuming) the download: check space, then confirm.
            long free = ModelManager.GetFreeSpaceBytes();
            if (free >= 0 && free < ModelManager.RequiredFreeBytes && page != null)
            {
                await page.DisplayAlertAsync("Not enough space",
                    $"The model needs about {Gb(ModelManager.RequiredFreeBytes)} free and this phone has {Gb(free)}. Free some space and try again.", "OK");
                return;
            }
            if (!ModelManager.HasPartialDownload() && page != null)
            {
                bool go = await page.DisplayAlertAsync("Download the AI model?",
                    "It's a one-time 2.4 GB download — Wi-Fi is best. Nova works normally while it downloads.",
                    "Start", "Not now");
                if (!go) return;
            }
            ModelManager.Start();
        }

        private async System.Threading.Tasks.Task OnSecondary()
        {
            // Only shown for a paused/failed download: throw the part-file away.
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            bool sure = await page.DisplayAlertAsync("Delete the partial download?",
                $"The {Gb(ModelManager.GetPartialSizeBytes())} downloaded so far will be lost.", "Delete", "Keep it");
            if (sure) { ModelManager.Stop(); Refresh(); }
        }

        private static string Gb(long bytes) => $"{bytes / 1_073_741_824.0:0.0} GB";
    }
}
