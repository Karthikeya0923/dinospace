namespace dinospace
{
    public partial class SpaceDetailPage : ContentPage
    {
        private SpaceObject _obj;
        private bool _animatedIn = false; // ensures the entrance animation only runs once

        public SpaceDetailPage(SpaceObject obj)
        {
            InitializeComponent();
            _obj = obj;

            // Record this view for the "Most viewed" stats
            StatsManager.RecordView(obj.Name);

            // Hero image (only set if one exists)
            if (!string.IsNullOrEmpty(obj.ImageFile))
                HeroImage.Source = obj.ImageFile;

            // Header labels
            NameLabel.Text = obj.Name;
            PronunciationLabel.Text = obj.Pronunciation;
            SubtitleLabel.Text = obj.Subtitle;

            // Type chip (e.g. "Planet", "Galaxy")
            ChipRow.Children.Add(UiHelpers.Chip(obj.TypeLabel));

            // Key stats grid (diameter, distance, mass, etc.)
            Stat1Label.Text = obj.Stat1Label; Stat1Value.Text = obj.Stat1Value;
            Stat2Label.Text = obj.Stat2Label; Stat2Value.Text = obj.Stat2Value;
            Stat3Label.Text = obj.Stat3Label; Stat3Value.Text = obj.Stat3Value;
            Stat4Label.Text = obj.Stat4Label; Stat4Value.Text = obj.Stat4Value;

            // Body text sections
            AboutLabel.Text = obj.AboutText;
            KeyFeaturesLabel.Text = obj.KeyFeaturesText;

            // Some objects have a History section instead of Orbit & Movement
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

            // Some objects have a "What's Inside" section instead of Surface & Composition
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

            // Hide and lower the page content ready for the entrance animation
            Content.Opacity = 0;
            Content.TranslationY = 14;
        }

        // Fade + rise entrance animation, runs once on first appear
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_animatedIn)
            {
                _animatedIn = true;
                await Task.WhenAll(
                    Content.FadeTo(1, 260, Easing.CubicOut),
                    Content.TranslateTo(0, 0, 260, Easing.CubicOut));
            }
        }

        // Show "Back to top" button once the user scrolls past the hero image
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

        // Sync bookmark icon with current saved state
        private void UpdateBookmarkIcon()
        {
            BookmarkImage.Source = SavedManager.IsSpaceSaved(_obj.Name)
                ? "bookmarkfilled.png"
                : "bookmark.png";
        }

        // Toggle saved state and refresh the icon
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