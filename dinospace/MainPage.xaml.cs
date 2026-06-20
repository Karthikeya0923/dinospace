using Microsoft.Maui.Storage;
namespace dinospace
{
    public partial class MainPage : ContentPage
    {
        // Navigation guard to prevent double-taps opening two detail pages
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;

        // Cached reference to the most-viewed entry for tap navigation
        private object _mostViewedObject = null;


        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Release nav guard only after a real return (not a spurious re-appear from pushing a detail page)
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;

            CloseDropdown();
            RefreshStats();
            BuildHighlights();
        }

        // ===== Today's Highlights =====
        // One dino, one space object, one fact — rotates daily using day-of-year as seed

        private void BuildHighlights()
        {
            HighlightStack.Children.Clear();

            var dinos = DinosaurData.GetAll().Where(IsFilledDino).ToList();
            var space = SpaceData.GetAll().Where(IsFilledSpace).ToList();
            var facts = ExploreFacts.Facts;

            // Same seed all day so picks stay consistent until midnight
            var rng = new Random(DateTime.Now.DayOfYear);

            if (dinos.Count > 0)
            {
                var d = dinos[rng.Next(dinos.Count)];
                HighlightStack.Children.Add(BuildHighlightRow("Dinosaur of the Day", d.ImageFile, d.Name, d.Era, d));
            }

            if (space.Count > 0)
            {
                var s = space[rng.Next(space.Count)];
                HighlightStack.Children.Add(BuildHighlightRow("Space Object of the Day", s.ImageFile, s.Name, s.Subtitle, s));
            }

            if (facts.Count > 0)
            {
                var fact = facts[rng.Next(facts.Count)];
                HighlightStack.Children.Add(BuildFactCard(fact));
            }
        }

        // Build one highlight row: thumbnail + tag/name/subtitle + chevron
        private View BuildHighlightRow(string tag, string imageFile, string name, string subtitle, object data)
        {
            var thumb = new Image
            {
                Source = imageFile,
                WidthRequest = 50,
                HeightRequest = 50,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Theme.ImgPlaceholder
            };

            var tagLabel = new Label { Text = tag, FontSize = 12, TextColor = Theme.TextSecondary };
            var nameLabel = new Label { Text = name, FontSize = 17, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo" };
            var subLabel = new Label { Text = subtitle, FontSize = 11, TextColor = Theme.TextSecondary };

            var info = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            info.Add(tagLabel); info.Add(nameLabel); info.Add(subLabel);

            var chevron = new Label { Text = "›", FontSize = 22, TextColor = Theme.TextHint, VerticalOptions = LayoutOptions.Center };

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(thumb, 0, 0); grid.Add(info, 1, 0); grid.Add(chevron, 2, 0);

            var frame = new Frame
            {
                Padding = new Thickness(12),
                CornerRadius = 14,
                BorderColor = Theme.Border,
                BackgroundColor = Theme.Surface,
                HasShadow = false,
                Content = grid,
                BindingContext = data
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnHighlightTapped;
            frame.GestureRecognizers.Add(tap);
            return frame;
        }

        // Build the "Did You Know?" fact card
        private View BuildFactCard(string fact)
        {
            var head = new Label { Text = "Did You Know?", FontSize = 12, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo", TextColor = Theme.TextSecondary };
            var body = new Label { Text = fact, FontSize = 14, LineHeight = 1.4, TextColor = Theme.TextPrimary };

            var stack = new VerticalStackLayout { Spacing = 4 };
            stack.Add(head); stack.Add(body);

            return new Frame
            {
                Padding = new Thickness(14),
                CornerRadius = 14,
                BorderColor = Theme.Border,
                BackgroundColor = Theme.Surface,
                HasShadow = false,
                Content = stack
            };
        }

        // ===== Home stats =====

        // Update streak counter and most-viewed entry
        private void RefreshStats()
        {
            int streak = StatsManager.UpdateAndGetStreak();
            StreakNumberLabel.Text = streak.ToString();
            StreakSubLabel.Text = streak == 1 ? "day in a row" : "days in a row";

            string topName = StatsManager.GetMostViewedName();
            if (string.IsNullOrEmpty(topName))
            {
                MostViewedNameLabel.Text = "None yet";
                MostViewedSubLabel.Text = "Open an entry to start";
                _mostViewedObject = null;
                return;
            }

            int count = StatsManager.GetViews(topName);
            MostViewedNameLabel.Text = topName;
            MostViewedSubLabel.Text = count == 1 ? "1 view" : $"{count} views";

            // Resolve name to a dino or space object for navigation on tap
            object obj = DinosaurData.GetAll().FirstOrDefault(d => d.Name == topName);
            if (obj == null) obj = SpaceData.GetAll().FirstOrDefault(s => s.Name == topName);
            _mostViewedObject = obj;
        }

        // Tap the most-viewed card to open that entry's detail page
        private async void OnMostViewedTapped(object sender, EventArgs e)
        {
            if (_isNavigating || _mostViewedObject == null) return;
            _isNavigating = true;
            _lastNav = DateTime.Now;
            if (_mostViewedObject is Dinosaur d)
                await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d), false);
            else if (_mostViewedObject is SpaceObject s)
                await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s), false);
        }

        // ===== Live search dropdown =====

        // Rebuild the dropdown as the user types
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string query = (e.NewTextValue ?? "").Trim().ToLower();
            DropdownStack.Children.Clear();

            if (string.IsNullOrEmpty(query)) { ShowDropdown(false); return; }

            // Up to 3 dinos + 3 space objects matching the query
            var dinos = DinosaurData.GetAll()
                .Where(d => d.Name.ToLower().Contains(query))
                .OrderBy(d => d.Name).Take(3).ToList();

            var space = SpaceData.GetAll()
                .Where(s => s.Name.ToLower().Contains(query))
                .OrderBy(s => s.Name).Take(3).ToList();

            if (dinos.Count == 0 && space.Count == 0) { ShowDropdown(false); return; }

            foreach (var d in dinos)
                DropdownStack.Children.Add(UiHelpers.BuildSearchResultRow(d.Name, "Dinosaur", d, OnDinoTapped));
            foreach (var s in space)
                DropdownStack.Children.Add(UiHelpers.BuildSearchResultRow(s.Name, s.TypeLabel, s, OnSpaceTapped));

            ShowDropdown(true);
        }

        private void ShowDropdown(bool show)
        {
            SearchDropdown.IsVisible = show;
            DismissLayer.IsVisible = show;
        }

        // Hide dropdown and clear the search field
        private void CloseDropdown()
        {
            SearchEntry.Text = "";
            DropdownStack.Children.Clear();
            ShowDropdown(false);
        }

        private void OnDismissTapped(object sender, EventArgs e)
        {
            CloseDropdown();
            SearchEntry.Unfocus();
        }

        // ===== Navigation =====

        private async void OnDinoTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is Dinosaur d)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                CloseDropdown();
                SearchEntry.Unfocus();
                await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d), false);
            }
        }

        private async void OnSpaceTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is SpaceObject s)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                CloseDropdown();
                SearchEntry.Unfocus();
                await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s), false);
            }
        }

        // Highlight row tap — routes to dino or space detail based on BindingContext type
        private async void OnHighlightTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v)
            {
                if (v.BindingContext is Dinosaur d)
                {
                    _isNavigating = true;
                    _lastNav = DateTime.Now;
                    await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d), false);
                }
                else if (v.BindingContext is SpaceObject s)
                {
                    _isNavigating = true;
                    _lastNav = DateTime.Now;
                    await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s), false);
                }
            }
        }

        // Home card navigation
        private async void OnDinoPediaTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true; _lastNav = DateTime.Now;
            await Shell.Current.Navigation.PushAsync(new DinoPediaPage());
        }

        private async void OnSpacePediaTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true; _lastNav = DateTime.Now;
            await Shell.Current.Navigation.PushAsync(new SpacePediaPage());
        }

        private async void OnAskAiTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true; _lastNav = DateTime.Now;
            await Shell.Current.Navigation.PushAsync(new AskAiPage());
        }

        private async void OnScanSkyTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true; _lastNav = DateTime.Now;
            await Shell.Current.Navigation.PushAsync(new ScanSkyPage());
        }

        // Only include entries that have real content (not placeholder text)
        private bool IsFilledDino(Dinosaur d) => !string.IsNullOrEmpty(d.AboutText) && !d.AboutText.StartsWith("Change");
        private bool IsFilledSpace(SpaceObject s) => !string.IsNullOrEmpty(s.AboutText) && !s.AboutText.StartsWith("Change");
    }
}