using System.Xml;

namespace dinospace
{
    public partial class SpaceDetailPage : ContentPage
    {
        private SpaceObject _obj;

        public SpaceDetailPage(SpaceObject obj)
        {
            InitializeComponent();
            SwipeBack.Attach(this);
            _obj = obj;

            StatsManager.RecordView(obj.Name);

            if (!string.IsNullOrEmpty(obj.ImageFile))
                HeroImage.Source = obj.ImageFile;

            NameLabel.Text = obj.Name;
            PronunciationLabel.Text = obj.Pronunciation;
            SubtitleLabel.Text = obj.Subtitle;

            ChipRow.Children.Add(UiHelpers.Chip(obj.TypeLabel));

            Stat1Label.Text = obj.Stat1Label; Stat1Value.Text = obj.Stat1Value;
            Stat2Label.Text = obj.Stat2Label; Stat2Value.Text = obj.Stat2Value;
            Stat3Label.Text = obj.Stat3Label; Stat3Value.Text = obj.Stat3Value;
            Stat4Label.Text = obj.Stat4Label; Stat4Value.Text = obj.Stat4Value;

            AboutLabel.Text = obj.AboutText;
            KeyFeaturesLabel.Text = obj.KeyFeaturesText;

            if (!string.IsNullOrEmpty(obj.HistoryText))
            {
                OrbitLabel.Text = obj.HistoryText;
                OrbitHeaderLabel.Text = "History";
            }
            else
            {
                OrbitLabel.Text = obj.OrbitMovementText;
                OrbitHeaderLabel.Text = "Orbit & Movement";
            }

            if (!string.IsNullOrEmpty(obj.WhatsInsideText))
            {
                SurfaceLabel.Text = obj.WhatsInsideText;
                SurfaceHeaderLabel.Text = "What's Inside";
            }
            else
            {
                SurfaceLabel.Text = obj.SurfaceCompositionText;
                SurfaceHeaderLabel.Text = "Surface & Composition";
            }

            FunFactsLabel.Text = obj.FunFactsText;

            UpdateBookmarkIcon();
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            bool show = e.ScrollY > 300;
            if (show != BackToTopButton.IsVisible)
                BackToTopButton.IsVisible = show;
        }

        private async void OnBackToTopClicked(object sender, EventArgs e)
        {
            await DetailScroll.ScrollToAsync(0, 0, true);
        }

        private void UpdateBookmarkIcon()
        {
            BookmarkImage.Source = SavedManager.IsSpaceSaved(_obj.Name) ? "bookmarkfilled.png" : "bookmark.png";
        }

        private void OnBookmarkClicked(object sender, EventArgs e)
        {
            if (SavedManager.IsSpaceSaved(_obj.Name))
                SavedManager.UnsaveSpace(_obj.Name);
            else
                SavedManager.SaveSpace(_obj.Name);

            UpdateBookmarkIcon();
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }
}