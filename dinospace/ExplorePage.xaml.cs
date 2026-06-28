using static Google.Android.Material.Tabs.TabLayout;

namespace dinospace
{
    public partial class ExplorePage : ContentView, ITabView
    {
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;
        private bool _showDino = true;
        private Dinosaur _featuredDino;
        private SpaceObject _featuredSpace;
        private readonly Random _rng = new Random();
        private bool _collectionsBuilt = false;

        public ExplorePage()
        {
            InitializeComponent();
        }

        public void OnSelected()
        {
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;

            ShowRandomFact();
            PickFeatured();
            _showDino = true;
            RefreshFeatured();
            RefreshProgress();

            if (!_collectionsBuilt)
            {
                _collectionsBuilt = true;
                Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(1);
                    BuildCollections();
                });
            }
        }

        private void ShowRandomFact()
        {
            DidYouKnowLabel.Text = ExploreFacts.Facts[_rng.Next(ExploreFacts.Facts.Count)];
        }

        private void OnDidYouKnowTapped(object sender, EventArgs e) => ShowRandomFact();

        private async void OnQuizTapped(object sender, EventArgs e)
        {
            string mode = await Shell.Current.DisplayActionSheet("Choose a Quiz", "Cancel", null, "Dinosaurs", "Space", "Mixed");
            if (mode != "Dinosaurs" && mode != "Space" && mode != "Mixed") return;

            string countChoice = await Shell.Current.DisplayActionSheet("How many questions?", "Cancel", null, "5 questions", "10 questions");
            if (countChoice != "5 questions" && countChoice != "10 questions") return;

            int count = countChoice == "5 questions" ? 5 : 10;
            await Shell.Current.Navigation.PushAsync(new QuizPage(mode, count));
        }

        private void PickFeatured()
        {
            var dinos = DinosaurData.GetAll().Where(IsFilledDino).ToList();
            var space = SpaceData.GetAll().Where(IsFilledSpace).ToList();

            if (dinos.Count > 0)
                _featuredDino = dinos[DateTime.Now.DayOfYear % dinos.Count];
            if (space.Count > 0)
                _featuredSpace = space[DateTime.Now.DayOfYear % space.Count];
        }

        private void RefreshFeatured()
        {
            if (_showDino && _featuredDino != null)
            {
                FeaturedImage.Source = _featuredDino.ImageFile;
                FeaturedTag.Text = "Dinosaur of the Day";
                FeaturedName.Text = _featuredDino.Name;
                FeaturedSubtitle.Text = _featuredDino.Era;
            }
            else if (!_showDino && _featuredSpace != null)
            {
                FeaturedImage.Source = _featuredSpace.ImageFile;
                FeaturedTag.Text = "Space Object of the Day";
                FeaturedName.Text = _featuredSpace.Name;
                FeaturedSubtitle.Text = _featuredSpace.Subtitle;
            }
        }

        private void OnFeaturedToggle(object sender, EventArgs e)
        {
            _showDino = !_showDino;
            RefreshFeatured();
        }

        private async void OnFeaturedTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            _lastNav = DateTime.Now;
            if (_showDino && _featuredDino != null)
                await Shell.Current.Navigation.PushAsync(new DinoDetailPage(_featuredDino));
            else if (!_showDino && _featuredSpace != null)
                await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(_featuredSpace));
        }

        private void RefreshProgress()
        {
            var dinos = DinosaurData.GetAll().Where(IsFilledDino).ToList();
            var space = SpaceData.GetAll().Where(IsFilledSpace).ToList();

            int dinoRead = dinos.Count(d => StatsManager.GetViews(d.Name) > 0);
            int spaceRead = space.Count(s => StatsManager.GetViews(s.Name) > 0);
            int saved = SavedManager.GetSavedDinos().Count + SavedManager.GetSavedSpace().Count;

            ProgressDinoCount.Text = $"{dinoRead}/{dinos.Count}";
            ProgressSpaceCount.Text = $"{spaceRead}/{space.Count}";
            ProgressSavedCount.Text = saved.ToString();

            ProgressDinoBest.Text = QuizAccuracy("Dinosaurs");
            ProgressSpaceBest.Text = QuizAccuracy("Space");
            ProgressMixedBest.Text = QuizAccuracy("Mixed");
        }

        private string QuizAccuracy(string mode)
        {
            int answered = Preferences.Get($"quiz_questions_{mode}", 0);
            if (answered == 0) return "—";
            int correct = Preferences.Get($"quiz_correct_{mode}", 0);
            int pct = (int)Math.Round(100.0 * correct / answered);
            return $"{pct}%";
        }

        private async void OnSurpriseTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            _lastNav = DateTime.Now;

            var dinos = DinosaurData.GetAll().Where(IsFilledDino).Cast<object>().ToList();
            var space = SpaceData.GetAll().Where(IsFilledSpace).Cast<object>().ToList();
            var all = dinos.Concat(space).ToList();

            if (all.Count > 0)
            {
                var pick = all[_rng.Next(all.Count)];
                if (pick is Dinosaur d)
                    await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d));
                else if (pick is SpaceObject s)
                    await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s));
            }
        }

        private async void OnCompareTapped(object sender, EventArgs e)
        {
            await Shell.Current.DisplayAlert("Coming Soon", "Comparing two dinosaurs or space objects is coming in a future update!", "OK");
        }

        private void BuildCollections()
        {
            CollectionsStack.Children.Add(MakeCollectionCard("Apex Predators", "The mightiest hunters, strongest first", "predators"));
            CollectionsStack.Children.Add(MakeCollectionCard("Speed Demons", "Fastest creatures, ranked", "speed"));
            CollectionsStack.Children.Add(MakeCollectionCard("Colossal Giants", "The heaviest of them all", "giants"));
            CollectionsStack.Children.Add(MakeCollectionCard("A Journey From Earth", "Space objects, nearest to farthest", "space_distance"));
            CollectionsStack.Children.Add(MakeCollectionCard("Cosmic Giants", "The largest objects in space", "space_size"));
        }

        private View MakeCollectionCard(string title, string desc, string id)
        {
            var name = new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold };
            var sub = new Label { Text = desc, FontSize = 12, TextColor = Theme.TextSecondary };
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(name); info.Add(sub);

            var chevron = new Label { Text = "›", FontSize = 22, TextColor = Theme.TextHint, VerticalOptions = LayoutOptions.Center };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(info, 0, 0);
            grid.Add(chevron, 1, 0);

            var frame = new Frame
            {
                Padding = new Thickness(16, 14),
                CornerRadius = 14,
                BorderColor = Theme.Border,
                BackgroundColor = Theme.Surface,
                HasShadow = false,
                Content = grid
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                if (_isNavigating) return;
                _isNavigating = true;
                _lastNav = DateTime.Now;
                await Shell.Current.Navigation.PushAsync(new CollectionPage(id));
            };
            frame.GestureRecognizers.Add(tap);
            return frame;
        }

        private bool IsFilledDino(Dinosaur d) => !string.IsNullOrEmpty(d.AboutText) && !d.AboutText.StartsWith("Change");
        private bool IsFilledSpace(SpaceObject s) => !string.IsNullOrEmpty(s.AboutText) && !s.AboutText.StartsWith("Change");
    }
}