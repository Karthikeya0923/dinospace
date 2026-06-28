namespace dinospace
{
    public partial class DinoPediaPage : ContentPage
    {
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;
        private bool _filtersOpen = false;
        private bool _built = false;

        private string _category = "All";
        private string _group = "All";
        private string _diet = "All";
        private string _size = "All";

        private const string SizeSmall = "Under 1,000 kg";
        private const string SizeMedium = "1,000-10,000 kg";
        private const string SizeLarge = "Over 10,000 kg";

        public DinoPediaPage()
        {
            InitializeComponent();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;

            if (!_built)
            {
                _built = true;
                // Wait for the push animation to finish before doing heavy build work,
                // so the slide-in isn't blocked and stutters.
                Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(280);
                    BuildFilterPanel();
                    UpdateFilterButton();
                    BuildList();
                });
            }
        }

        private void OnFilterToggle(object sender, EventArgs e)
        {
            _filtersOpen = !_filtersOpen;
            FilterPanel.IsVisible = _filtersOpen;
        }

        private void BuildFilterPanel()
        {
            FilterChips.Children.Clear();

            FilterChips.Children.Add(MakeDimension("Category", new[] { "All", "Land", "Flying", "Sea" }, _category, v => { _category = v; OnFilterChanged(); }));

            var groups = new List<string> { "All" };
            groups.AddRange(DinosaurData.GetAll().Select(d => d.Group).Distinct().OrderBy(g => g));
            FilterChips.Children.Add(MakeDimension("Group", groups.ToArray(), _group, v => { _group = v; OnFilterChanged(); }));

            FilterChips.Children.Add(MakeDimension("Diet", new[] { "All", "Carnivore", "Herbivore" }, _diet, v => { _diet = v; OnFilterChanged(); }));

            FilterChips.Children.Add(MakeDimension("Size", new[] { "All", SizeSmall, SizeMedium, SizeLarge }, _size, v => { _size = v; OnFilterChanged(); }));
        }

        private void OnFilterChanged()
        {
            BuildFilterPanel();
            UpdateFilterButton();
            BuildList();
        }

        private void UpdateFilterButton()
        {
            int active = 0;
            if (_category != "All") active++;
            if (_group != "All") active++;
            if (_diet != "All") active++;
            if (_size != "All") active++;
            FilterButton.Text = active == 0 ? "Filters" : $"Filters ({active})";
        }

        private View MakeDimension(string title, string[] options, string current, Action<string> onSelect)
        {
            var head = new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo", TextColor = Theme.TextSecondary };

            var row = new HorizontalStackLayout { Spacing = 8 };
            foreach (var opt in options)
                row.Add(MakeChip(opt, opt == current, onSelect));

            var scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row };

            var box = new VerticalStackLayout { Spacing = 6 };
            box.Add(head);
            box.Add(scroll);
            return box;
        }

        private View MakeChip(string text, bool selected, Action<string> onSelect)
        {
            var label = new Label { Text = text, FontSize = 13, TextColor = selected ? Colors.Black : Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };

            var chip = new Frame { Padding = new Thickness(12, 6), CornerRadius = 14, HasShadow = false, BorderColor = Colors.Transparent, BackgroundColor = selected ? Theme.Accent : Theme.ChipBg, Content = label };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => onSelect(text);
            chip.GestureRecognizers.Add(tap);
            return chip;
        }

        private void OnSearchCompleted(object sender, EventArgs e) => BuildList();
        private void OnSearchChanged(object sender, TextChangedEventArgs e) => BuildList();

        private void BuildList()
        {
            string query = SearchEntry.Text?.Trim().ToLower() ?? "";

            var dinos = DinosaurData.GetAll()
                .Where(d => _category == "All" || d.Category == _category)
                .Where(d => _group == "All" || d.Group == _group)
                .Where(d => _diet == "All" || d.Diet == _diet)
                .Where(d => SizeMatch(d))
                .Where(d => string.IsNullOrEmpty(query) || d.Name.ToLower().Contains(query))
                .OrderBy(d => d.Name)
                .ToList();

            ResultsList.ItemsSource = dinos;
        }

        private bool SizeMatch(Dinosaur d)
        {
            if (_size == "All") return true;
            double w = ParseWeight(d.Weight);
            if (_size == SizeSmall) return w > 0 && w < 1000;
            if (_size == SizeMedium) return w >= 1000 && w <= 10000;
            if (_size == SizeLarge) return w > 10000;
            return true;
        }

        private double ParseWeight(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var cleaned = s.Replace(",", "");
            var sb = new System.Text.StringBuilder();
            foreach (char c in cleaned)
            {
                if (char.IsDigit(c) || c == '.') sb.Append(c);
                else if (sb.Length > 0) break;
            }
            return double.TryParse(sb.ToString(), out var v) ? v : 0;
        }

        // Tap a row -> read the bound Dinosaur off its BindingContext and open it.
        private async void OnDinoTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is Dinosaur d)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d));
            }
        }
    }
}