namespace dinospace
{
    public partial class DinoDetailPage : ContentPage
    {
        private Dinosaur _dino;
        private bool _animatedIn = false; // ensures the entrance animation only runs once

        public DinoDetailPage(Dinosaur dino)
        {
            InitializeComponent();
            _dino = dino;

            // Record this view for the "Most viewed" stats
            StatsManager.RecordView(dino.Name);

            // Hero image (only set if one exists)
            if (!string.IsNullOrEmpty(dino.ImageFile))
                HeroImage.Source = dino.ImageFile;

            // Header labels
            NameLabel.Text = dino.Name;
            PronunciationLabel.Text = dino.Pronunciation;
            MeaningLabel.Text = string.IsNullOrEmpty(dino.Meaning) ? "" : $"Meaning: {dino.Meaning}";
            EraLabel.Text = dino.Era;

            // Diet + group chips below the name
            ChipRow.Children.Add(UiHelpers.Chip(dino.Diet));
            ChipRow.Children.Add(UiHelpers.Chip(dino.Group));

            // Flying creatures show wingspan instead of body length
            LengthHeader.Text = dino.Category == "Flying" ? "Wingspan" : "Length";
            LengthLabel.Text = dino.Length;
            WeightLabel.Text = dino.Weight;
            SpeedLabel.Text = dino.Speed;

            // Show width if available, otherwise height
            if (!string.IsNullOrEmpty(dino.Width))
            {
                HeightWidthLabel.Text = "Width";
                HeightWidthValue.Text = dino.Width;
            }
            else
            {
                HeightWidthLabel.Text = "Height";
                HeightWidthValue.Text = dino.Height;
            }

            // Body text sections
            AboutLabel.Text = dino.AboutText;
            KeyFeaturesLabel.Text = dino.KeyFeaturesText;
            LifeEnvironmentLabel.Text = dino.LifeEnvironmentText;
            BehaviourLabel.Text = dino.BehaviourText;
            FunFactsLabel.Text = dino.FunFactsText;

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
            BookmarkImage.Source = SavedManager.IsDinoSaved(_dino.Name)
                ? "bookmarkfilled.png"
                : "bookmark.png";
        }

        // Toggle saved state and refresh the icon
        private void OnBookmarkClicked(object sender, EventArgs e)
        {
            if (SavedManager.IsDinoSaved(_dino.Name))
                SavedManager.UnsaveDino(_dino.Name);
            else
                SavedManager.SaveDino(_dino.Name);

            UpdateBookmarkIcon();
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }
}