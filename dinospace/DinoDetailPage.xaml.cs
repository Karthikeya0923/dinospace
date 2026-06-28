using System.Xml;

namespace dinospace
{
    public partial class DinoDetailPage : ContentPage
    {
        private Dinosaur _dino;

        public DinoDetailPage(Dinosaur dino)
        {
            InitializeComponent();
            SwipeBack.Attach(this);
            _dino = dino;

            StatsManager.RecordView(dino.Name);

            if (!string.IsNullOrEmpty(dino.ImageFile))
                HeroImage.Source = dino.ImageFile;

            NameLabel.Text = dino.Name;
            PronunciationLabel.Text = dino.Pronunciation;
            MeaningLabel.Text = string.IsNullOrEmpty(dino.Meaning) ? "" : $"Meaning: {dino.Meaning}";
            EraLabel.Text = dino.Era;

            ChipRow.Children.Add(UiHelpers.Chip(dino.Diet));
            ChipRow.Children.Add(UiHelpers.Chip(dino.Group));

            LengthHeader.Text = dino.Category == "Flying" ? "Wingspan" : "Length";
            LengthLabel.Text = dino.Length;
            WeightLabel.Text = dino.Weight;
            SpeedLabel.Text = dino.Speed;

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

            AboutLabel.Text = dino.AboutText;
            KeyFeaturesLabel.Text = dino.KeyFeaturesText;
            LifeEnvironmentLabel.Text = dino.LifeEnvironmentText;
            BehaviourLabel.Text = dino.BehaviourText;
            FunFactsLabel.Text = dino.FunFactsText;

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
            BookmarkImage.Source = SavedManager.IsDinoSaved(_dino.Name) ? "bookmarkfilled.png" : "bookmark.png";
        }

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